using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public enum ChannelType { Master, BGM, KeySound, Sfx, Count };

public enum SoundBuffer { _64, _128, _256, _512, _1024, Count, }

public enum SoundSfxType 
{ 
    MainSelect, MainClick, MainHover, Slider,
    MenuSelect, MenuClick, MenuHover
}

public class SoundManager : SingletonUnity<SoundManager>
{
    #region variables
    private FMOD.System system;

    private static readonly int MaxNameLength = 256;
    private static readonly int MaxSoftwareChnnels = 128;
    private static readonly int MaxVirtualChannels = 1000;
    private Dictionary<ChannelType, FMOD.ChannelGroup> groups = new Dictionary<ChannelType, FMOD.ChannelGroup>();
    private Dictionary<SoundSfxType, FMOD.Sound> sfxSounds = new Dictionary<SoundSfxType, FMOD.Sound>();
    private Dictionary<string, FMOD.Sound> keySounds = new Dictionary<string, FMOD.Sound>();

    private int totalKeySoundCount;
    public int TotalKeySoundCount => totalKeySoundCount;
    public int KeySoundCount => keySounds.Count;

    private FMOD.Sound bgmSound;
    private FMOD.Channel bgmChannel;
    private FMOD.DSP Multiband;

    public struct SoundDriver
    {
        public System.Guid guid;
        public int index;
        public string name;
        public int systemRate, speakModeChannels;
        public FMOD.SPEAKERMODE mode;
    }
    public ReadOnlyCollection<SoundDriver> SoundDrivers { get; private set; }
    public int CurrentDriverIndex 
    {
        get => curDriverIndex;
        set
        {
            int curIndex;
            ErrorCheck( system.getDriver( out curIndex ) );

            if ( SoundDrivers.Count <= value || curIndex == value )
            {
                Debug.LogWarning( "SoundDriver Index is Out of Range or Duplicated Value" );
                return;
            }

            ErrorCheck( system.setDriver( value ) );
            curDriverIndex = value;
        }
    }
    private int curDriverIndex;

    public uint Position
    {
        get
        {
            if ( !IsPlaying( ChannelType.BGM ) )
            {
                Debug.LogError( "bgm is not playing" );
                return 0;
            }

            uint pos;
            ErrorCheck( bgmChannel.getPosition( out pos, FMOD.TIMEUNIT.MS ) );
            return pos;
        }

        set
        {
            if ( !IsPlaying( ChannelType.BGM ) )
            {
                Debug.LogError( "bgm is not playing" );
                return;
            }
            
            ErrorCheck( bgmChannel.setPosition( value, FMOD.TIMEUNIT.MS ) );
        }
    }
    public uint Length
    {
        get
        {
            if ( !hasAccurateTime && !bgmSound.hasHandle() )
            {
                Debug.LogError( $"Doesn't have AccurateTime Flag. or BGM is not playing" );
                return 0;
            }

            uint length;
            ErrorCheck( bgmSound.getLength( out length, FMOD.TIMEUNIT.MS ) );
            return length;
        }
    }

    public int UseChannelCount
    {
        get
        {
            int channels = 0;
            ErrorCheck( system.getChannelsPlaying( out channels ) );
            return channels;
        }
    }

    private bool hasAccurateTime = false;

    public event Action OnSoundSystemReLoad;
    public event Action OnRelease;
    public bool IsLoad { get; private set; } = false;
    #endregion

    #region System
    public void Initialize()
    {
        // System
        ErrorCheck( FMOD.Factory.System_Create( out system ) );
        ErrorCheck( system.setOutput( FMOD.OUTPUTTYPE.AUTODETECT ) );
        
        // To do Before System Initialize
        int samplerRate, numRawSpeakers;
        FMOD.SPEAKERMODE mode;
        ErrorCheck( system.getSoftwareFormat( out samplerRate, out mode, out numRawSpeakers ) );
        ErrorCheck( system.setSoftwareFormat( samplerRate, FMOD.SPEAKERMODE.STEREO, numRawSpeakers ) );

        ErrorCheck( system.getSoftwareFormat( out samplerRate, out mode, out numRawSpeakers ) );
        Debug.Log( $"SampleRate : {samplerRate} Mode : {mode} numRawSpeakers : {numRawSpeakers}" );

        ErrorCheck( system.setSoftwareChannels( MaxSoftwareChnnels ) );
        int softwareChannels;
        ErrorCheck( system.getSoftwareChannels( out softwareChannels ) );
        Debug.Log( $"SoftwareChannel {softwareChannels}" );

        var bufferText  = SystemSetting.CurrentSoundBuffer.ToString().Replace( "_", " " ).Trim();
        uint bufferSize = uint.Parse( bufferText );
        ErrorCheck( system.setDSPBufferSize( bufferSize, 4 ) );

        int numbuffers;
        ErrorCheck( system.getDSPBufferSize( out bufferSize, out numbuffers ) );
        Debug.Log( $"buffer size : {bufferSize} numbuffers : {numbuffers}" );

        // System Initialize
        IntPtr extraDriverData = new IntPtr();
        ErrorCheck( system.init( MaxVirtualChannels, FMOD.INITFLAGS.NORMAL, extraDriverData ) );
        uint version;
        ErrorCheck( system.getVersion( out version ) );
        if ( version < FMOD.VERSION.number )
            Debug.LogError( "using the old version." );

        // Sound Driver
        int numDriver;
        ErrorCheck( system.getNumDrivers( out numDriver ) );
        List<SoundDriver> drivers = new List<SoundDriver>();
        for ( int i = 0; i < numDriver; i++ )
        {
            SoundDriver driver;
            if ( ErrorCheck( system.getDriverInfo( i, out driver.name, MaxNameLength, out driver.guid, out driver.systemRate, out driver.mode, out driver.speakModeChannels ) ) )
            {
                driver.index = i;
                drivers.Add( driver );
            }
        }
        SoundDrivers = new ReadOnlyCollection<SoundDriver>( drivers );
        ErrorCheck( system.getDriver( out curDriverIndex ) );
        Debug.Log( $"Current Sound Device : {SoundDrivers[curDriverIndex].name}" );

        // ChannelGroup
        for ( int i = 0; i < ( int )ChannelType.Count; i++ )
        {
            FMOD.ChannelGroup group;
            ChannelType type = ( ChannelType )i;

            ErrorCheck( system.createChannelGroup( type.ToString(), out group ) );
            if ( type != ChannelType.Master )
                ErrorCheck( groups[ChannelType.Master].addGroup( group ) );

            groups.Add( type, group );
        }

        // Sfx Sound
        LoadSfx( SoundSfxType.MainClick, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\MainClick.wav" );
        LoadSfx( SoundSfxType.MenuClick, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\MenuClick.wav" );
        
        LoadSfx( SoundSfxType.MainSelect, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\MainSelect.wav" );
        LoadSfx( SoundSfxType.MenuSelect, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\MenuSelect.wav" );

        LoadSfx( SoundSfxType.MainHover, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\MainHover.wav" );
        LoadSfx( SoundSfxType.MenuHover, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\MenuHover.wav" );

        LoadSfx( SoundSfxType.Slider, @$"{Application.streamingAssetsPath}\\Default\\Sounds\\Sfx\\Slider.wav" );

        // DSP
        CreateLowEffectDsp();

        // Details
        SetVolume( .3f, ChannelType.Master );
        SetVolume( .3f, ChannelType.BGM );
        SetVolume( .3f, ChannelType.KeySound );
        SetVolume( .7f, ChannelType.Sfx );
    }

    public void KeyRelease()
    {
        totalKeySoundCount = 0;

        foreach ( var keySound in keySounds )
        {
            var sound = keySound.Value;
            if ( sound.hasHandle() )
            {
                ErrorCheck( sound.release() );
                sound.clearHandle();
            }
        }
        keySounds.Clear();
    }

    public void Release()
    {
        // Sounds
        foreach ( var sfx in sfxSounds.Values )
        {
            if ( sfx.hasHandle() )
            {
                ErrorCheck( sfx.release() );
                sfx.clearHandle();
            }
        }
        sfxSounds.Clear();

        foreach ( var keySound in keySounds )
        {
            var sound = keySound.Value;
            if ( sound.hasHandle() )
            {
                ErrorCheck( sound.release() );
                sound.clearHandle();
            }
        }
        keySounds.Clear();

        if ( bgmSound.hasHandle() )
        {
            ErrorCheck( bgmSound.release() );
            bgmSound.clearHandle();
        }

        // DSP
        int numPrevDSP, numCurrentDSP;
        ErrorCheck( groups[ChannelType.BGM].getNumDSPs( out numPrevDSP ) );

        OnRelease?.Invoke();
        RemoveDSP( ref Multiband );

        ErrorCheck( groups[ChannelType.BGM].getNumDSPs( out numCurrentDSP ) );
        Debug.Log( $"DSP Count : {numPrevDSP} -> {numCurrentDSP}" );

        // ChannelGroup
        for ( int i = 1; i < ( int )ChannelType.Count; i++ )
        {
            ErrorCheck( groups[( ChannelType )i].release() );
        }
        ErrorCheck( groups[ChannelType.Master].release() );
        groups.Clear();

        // System
        if ( system.hasHandle() )
        {
            ErrorCheck( system.release() ); // ???????? close ??.
            system.clearHandle();
        }
    }

    public void ReLoad()
    {
        AllStop();
        IsLoad = true;

        int prevDriverIndex;
        ErrorCheck( system.getDriver( out prevDriverIndex ) );

        Release();
        Initialize();

        OnSoundSystemReLoad?.Invoke();
        ErrorCheck( system.setDriver( prevDriverIndex ) );
        curDriverIndex = prevDriverIndex;

        IsLoad = false;
    }
    #endregion

    #region Unity Callback
    private void Awake() => Initialize();
    private void Update()
    {
        if ( !IsLoad ) 
             system.update();
    }

    private void OnDestroy()
    {
        // ???????? ???????? ???? ???????? ?????????? ????.
        // OnApplicationQuit -> OnDisable -> OnDestroy ?????? ???? ???? ??????
        // ?? ?????????? OnDisable, OnApplicationQuit?? ?????? ???? ?????? ???? ??
        // SoundManager OnDestroy?? ?????? ?? ?????? ????.
        Release();
    }
    #endregion

    #region Load
    public void LoadBgm( string _path, bool _isLoop, bool _isStream, bool _hasAccurateTime )
    {
        hasAccurateTime = _hasAccurateTime;

        FMOD.MODE mode = FMOD.MODE.CREATESAMPLE;
        mode = _hasAccurateTime ? mode |= FMOD.MODE.ACCURATETIME : mode;
        mode = _isLoop          ? mode |= FMOD.MODE.LOOP_NORMAL  : mode |= FMOD.MODE.LOOP_OFF;
        mode = _isStream        ? mode |= ( mode &= ~FMOD.MODE.CREATESAMPLE ) | FMOD.MODE.CREATESTREAM | FMOD.MODE.LOWMEM : mode;

        FMOD.Sound sound;
        ErrorCheck( system.createSound( _path, mode, out sound ) ); 

        if ( bgmSound.hasHandle() )
        {
            ErrorCheck( bgmSound.release() );
            bgmSound.clearHandle();
        }
        bgmSound = sound;
    }

    private void LoadSfx( SoundSfxType _type, string _path )
    {
        if ( sfxSounds.ContainsKey( _type ) )
        {
            Debug.LogError( $"sfxSound[{_type}] is duplicate loaded." );
            return;
        }

        FMOD.Sound sound;
        ErrorCheck( system.createSound( _path, FMOD.MODE.LOOP_OFF | FMOD.MODE.CREATESAMPLE, out sound ) );
        sfxSounds.Add( _type, sound );
    }

    public void LoadKeySound( string _path, out FMOD.Sound _sound )
    {
        var name = System.IO.Path.GetFileName( _path );
        if ( keySounds.ContainsKey( name ) )
        {
            ++totalKeySoundCount;
            _sound = keySounds[name];
            return;
        }

        if ( !System.IO.File.Exists( @_path ) )
             throw new Exception( $"File Exists  {_path}" );

        ErrorCheck( system.createSound( _path, FMOD.MODE.LOOP_OFF | FMOD.MODE.CREATESAMPLE, out _sound ) );
        keySounds.Add( name, _sound );
        ++totalKeySoundCount;
    }
    #endregion

    #region Play
    /// <summary> Play Background Music </summary>
    public void Play( bool _isPause = false )
    {
        if ( !bgmSound.hasHandle() )
        {
            Debug.LogError( "Bgm is not loaded." );
            return;
        }

        Stop( ChannelType.BGM );

        SetPaused( _isPause, ChannelType.BGM );
        ErrorCheck( system.playSound( bgmSound, groups[ChannelType.BGM], false, out bgmChannel ) );
    }

    /// <summary> Play Sound Special Effects </summary>
    public void Play( SoundSfxType _type )
    {
        if ( !sfxSounds.ContainsKey( _type ) )
        {
            Debug.LogError( $"sfxSound[{_type}] is not loaded." );
            return;
        }

        FMOD.Channel channel;
        ErrorCheck( system.playSound( sfxSounds[_type], groups[ChannelType.Sfx], false, out channel ) );
    }

    /// <summary> Play Key Sound Effects </summary>
    public void Play( in KeySound _keySound )
    {
        if ( !_keySound.hasSound )
             return;

        if ( !_keySound.sound.hasHandle() )
        {
            Debug.LogError( $"keySound[{_keySound.name}] is not loaded." );
            return;
        }

        FMOD.Channel channel;
        ErrorCheck( system.playSound( _keySound.sound, groups[ChannelType.KeySound], false, out channel ) );
        ErrorCheck( channel.setVolume( _keySound.volume ) );
    }
    #endregion

    #region ChannelGroup
    public bool IsPlaying( ChannelType _type )
    {
        bool isPlay = false;
        ErrorCheck( groups[_type].isPlaying( out isPlay ) );
        
        return isPlay;
    }

    public void SetPaused( bool _isPause, ChannelType _type ) => ErrorCheck( groups[_type].setPaused( _isPause ) );

    public float GetVolume( ChannelType _type )
    {
        float volume = 0f;
        ErrorCheck( groups[_type].getVolume( out volume ) );

        return volume;
    }

    public void SetVolume( float _value, ChannelType _type )
    {
        float volume = _value;
        if ( _value < 0f ) volume = 0f;
        if ( _value > 1f ) volume = 1f;

        ErrorCheck( groups[_type].setVolume( volume ) );
    }
    public void Stop( ChannelType _type ) => ErrorCheck( groups[_type].stop() );

    public void AllStop()
    {
        foreach ( var group in groups )
        {
            if ( IsPlaying( group.Key ) )
                ErrorCheck( group.Value.stop() );
        }
    }
    #endregion

    #region DSP
    public void AddFFT( int _size, FMOD.DSP_FFT_WINDOW _type, out FMOD.DSP _dsp )
    {
        ErrorCheck( system.createDSPByType( FMOD.DSP_TYPE.FFT, out _dsp ) );
        ErrorCheck( _dsp.setParameterInt( ( int )FMOD.DSP_FFT.WINDOWSIZE, _size ) );
        ErrorCheck( _dsp.setParameterInt( ( int )FMOD.DSP_FFT.WINDOWTYPE, ( int )_type ) );
        // ???? DSP ???? ???? ?????? ?????? FFT?? ???? BGM???? FFT ?????? ???????? TAIL?? ????.
        ErrorCheck( groups[ChannelType.BGM].addDSP( FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, _dsp ) );
    }

    public void RemoveDSP( ref FMOD.DSP _dsp, ChannelType _type = ChannelType.BGM )
    {
        if ( !_dsp.hasHandle() )
             return;

        ErrorCheck( groups[_type].removeDSP( _dsp ) );
        ErrorCheck( _dsp.release() );
        _dsp.clearHandle();
    }

    /// A ~ E  5 bands 
    /// 1. filter( int ) Default = FMOD_DSP_MULTIBAND_EQ_FILTER.LOWPASS_12DB
    /// 2. frequency( float ) Default = 8000, Range = 20 ~ 22000
    ///    ?????? ???? ??????
    /// 3. quality factor( float ) Default = 0.707, Range = 0.1 ~ 10
    ///    ?????? ????
    ///    resonance (low/high pass), bandwidth (notch, peaking, band-pass), phase transition sharpness (all-pass), unused (low/high shelf)
    /// 4. gain( float ) Default = 0, Range = -30 ~ 30, Unit = Decibels( dB )
    ///    ?????? ?????? ????, ????
    ///    Boost or attenuation [high/low shelf and peaking only]
    private void CreateLowEffectDsp()
    {
        ErrorCheck( system.createDSPByType( FMOD.DSP_TYPE.MULTIBAND_EQ, out Multiband ) );

        // multiband ?????? ???? ????
        //int numParameters = 0;
        //ErrorCheck( Multiband.getNumParameters( out numParameters ) );
        //FMOD.DSP_PARAMETER_DESC[] descs = new FMOD.DSP_PARAMETER_DESC[numParameters];
        //for ( int i = 0; i < numParameters; i++ )
        //{
        //    ErrorCheck( Multiband.getParameterInfo( i, out descs[i] ) );
        //    Debug.Log( $"Desc[{i}] Name        : { System.Text.Encoding.Default.GetString( descs[i].name ) }" );
        //    Debug.Log( $"Desc[{i}] Label       : { System.Text.Encoding.Default.GetString( descs[i].label ) }" );
        //    Debug.Log( $"Desc[{i}] Description : { descs[i].description }" );
        //    Debug.Log( $"Desc[{i}] Type        : { descs[i].type }" );
        //}

        ErrorCheck( Multiband.setParameterInt( ( int )FMOD.DSP_MULTIBAND_EQ.A_FILTER, ( int )FMOD.DSP_MULTIBAND_EQ_FILTER_TYPE.LOWSHELF ) );
        ErrorCheck( Multiband.setParameterInt( ( int )FMOD.DSP_MULTIBAND_EQ.B_FILTER, ( int )FMOD.DSP_MULTIBAND_EQ_FILTER_TYPE.LOWPASS_12DB ) );
        ErrorCheck( Multiband.setParameterInt( ( int )FMOD.DSP_MULTIBAND_EQ.C_FILTER, ( int )FMOD.DSP_MULTIBAND_EQ_FILTER_TYPE.LOWPASS_12DB ) );
        ErrorCheck( Multiband.setParameterInt( ( int )FMOD.DSP_MULTIBAND_EQ.D_FILTER, ( int )FMOD.DSP_MULTIBAND_EQ_FILTER_TYPE.LOWPASS_12DB ) );
        ErrorCheck( Multiband.setParameterInt( ( int )FMOD.DSP_MULTIBAND_EQ.E_FILTER, ( int )FMOD.DSP_MULTIBAND_EQ_FILTER_TYPE.LOWPASS_12DB ) );

        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.A_FREQUENCY, 320f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.B_FREQUENCY, 5000f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.C_FREQUENCY, 6000f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.D_FREQUENCY, 7000f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.E_FREQUENCY, 8000f ) );

        //ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.A_Q, .1f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.B_Q, .11f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.C_Q, .11f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.D_Q, .11f ) );
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.E_Q, .11f ) );
                    
        ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.A_GAIN, 10f ) );
        //ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.B_GAIN, 4f ) );
        //ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.C_GAIN, 4f ) );
        //ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.D_GAIN, 4f ) );
        //ErrorCheck( Multiband.setParameterFloat( ( int )FMOD.DSP_MULTIBAND_EQ.E_GAIN, 4f ) );
    }

    public void UseLowEqualizer( bool _isUse )
    {
        var bgmGroup = groups[ChannelType.BGM];
        int numDsp;
        ErrorCheck( bgmGroup.getNumDSPs( out numDsp ) );
        for ( int i = 0; i < numDsp; i++ )
        {
            FMOD.DSP dsp;
            ErrorCheck( bgmGroup.getDSP( i, out dsp ) );

            bool isEquals = Equals( dsp, Multiband );
            if ( isEquals && _isUse == true ) // ???? ?????? ????
            {
                return;
            }
            else if ( isEquals && _isUse == false )
            {
                ErrorCheck( bgmGroup.removeDSP( Multiband ) );
                return;
            }
        }

        // ?????? dsp?? ?????? ??????.
        if ( _isUse == true )
             ErrorCheck( bgmGroup.addDSP( FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, Multiband ) );
    }
    #endregion

    private bool ErrorCheck( FMOD.RESULT _res )
    {
        if ( FMOD.RESULT.OK != _res )
        {
            Debug.LogError( FMOD.Error.String( _res ) );
            return false;
        }

        return true;
    }
}
