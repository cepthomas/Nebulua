
-- Create the namespace/module.
local M = {}

-- The GM midi instrument definitions.
M.instruments = {
    AcousticGrandPiano = 000, BrightAcousticPiano = 001, ElectricGrandPiano = 002, HonkyTonkPiano = 003, ElectricPiano1 = 004, ElectricPiano2 = 005,
    Harpsichord = 006, Clavinet = 007, Celesta = 008, Glockenspiel = 009, MusicBox = 010, Vibraphone = 011,
    Marimba = 012, Xylophone = 013, TubularBells = 014, Dulcimer = 015, DrawbarOrgan = 016, PercussiveOrgan = 017,
    RockOrgan = 018, ChurchOrgan = 019, ReedOrgan = 020, Accordion = 021, Harmonica = 022, TangoAccordion = 023,
    AcousticGuitarNylon = 024, AcousticGuitarSteel = 025, ElectricGuitarJazz = 026, ElectricGuitarClean = 027, ElectricGuitarMuted = 028, OverdrivenGuitar = 029,
    DistortionGuitar = 030, GuitarHarmonics = 031, AcousticBass = 032, ElectricBassFinger = 033, ElectricBassPick = 034, FretlessBass = 035,
    SlapBass1 = 036, SlapBass2 = 037, SynthBass1 = 038, SynthBass2 = 039, Violin = 040, Viola = 041,
    Cello = 042, Contrabass = 043, TremoloStrings = 044, PizzicatoStrings = 045, OrchestralHarp = 046, Timpani = 047,
    StringEnsemble1 = 048, StringEnsemble2 = 049, SynthStrings1 = 050, SynthStrings2 = 051, ChoirAahs = 052, VoiceOohs = 053,
    SynthVoice = 054, OrchestraHit = 055, Trumpet = 056, Trombone = 057, Tuba = 058, MutedTrumpet = 059,
    FrenchHorn = 060, BrassSection = 061, SynthBrass1 = 062, SynthBrass2 = 063, SopranoSax = 064, AltoSax = 065,
    TenorSax = 066, BaritoneSax = 067, Oboe = 068, EnglishHorn = 069, Bassoon = 070, Clarinet = 071,
    Piccolo = 072, Flute = 073, Recorder = 074, PanFlute = 075, BlownBottle = 076,
    Shakuhachi = 077, Whistle = 078, Ocarina = 079, Lead1Square = 080, Lead2Sawtooth = 081,
    Lead3Calliope = 082, Lead4Chiff = 083, Lead5Charang = 084, Lead6Voice = 085, Lead7Fifths = 086, Lead8BassAndLead = 087,
    Pad1NewAge = 088, Pad2Warm = 089, Pad3Polysynth = 090, Pad4Choir = 091, Pad5Bowed = 092, Pad6Metallic = 093,
    Pad7Halo = 094, Pad8Sweep = 095, Fx1Rain = 096, Fx2Soundtrack = 097, Fx3Crystal = 098, Fx4Atmosphere = 099,
    Fx5Brightness = 100, Fx6Goblins = 101, Fx7Echoes = 102, Fx8SciFi = 103, Sitar = 104, Banjo = 105,
    Shamisen = 106, Koto = 107, Kalimba = 108, BagPipe = 109, Fiddle = 110, Shanai = 111,
    TinkleBell = 112, Agogo = 113, SteelDrums = 114, Woodblock = 115, TaikoDrum = 116, MelodicTom = 117,
    SynthDrum = 118, ReverseCymbal = 119, GuitarFretNoise = 120, BreathNoise = 121, Seashore = 122, BirdTweet = 123,
TelephoneRing = 124, Helicopter = 125, Applause = 126, Gunshot = 127}

-- The GM midi drum kit definitions.
M.drumkits =
{
    Standard = 0, Room = 8, Power = 16, Electronic = 24, TR808 = 25, Jazz = 32, Brush = 40, Orchestra = 48, SFX = 56
}

-- The GM midi drum definitions.
M.drums =
{
    AcousticBassDrum = 035,
    BassDrum1 = 036,
    SideStick = 037,
    AcousticSnare = 038,
    HandClap = 039,
    ElectricSnare = 040,
    LowFloorTom = 041,
    ClosedHiHat = 042,
    HighFloorTom = 043,
    PedalHiHat = 044,
    LowTom = 045,
    OpenHiHat = 046,
    LowMidTom = 047,
    HiMidTom = 048,
    CrashCymbal1 = 049,
    HighTom = 050,
    RideCymbal1 = 051,
    ChineseCymbal = 052,
    RideBell = 053,
    Tambourine = 054,
    SplashCymbal = 055,
    Cowbell = 056,
    CrashCymbal2 = 057,
    Vibraslap = 058,
    RideCymbal2 = 059,
    HiBongo = 060,
    LowBongo = 061,
    MuteHiConga = 062,
    OpenHiConga = 063,
    LowConga = 064,
    HighTimbale = 065,
    LowTimbale = 066,
    HighAgogo = 067,
    LowAgogo = 068,
    Cabasa = 069,
    Maracas = 070,
    ShortWhistle = 071,
    LongWhistle = 072,
    ShortGuiro = 073,
    LongGuiro = 074,
    Claves = 075,
    HiWoodBlock = 076,
    LowWoodBlock = 077,
    MuteCuica = 078,
    OpenCuica = 079,
    MuteTriangle = 080,
    OpenTriangle = 081,
}

-- The midi controller definitions.
controllers =
{
    BankSelect = 000,
    Modulation = 001,
    BreathController = 002,
    FootController = 004,
    PortamentoTime = 005,
    Volume = 007,
    Balance = 008,
    Pan = 010,
    Expression = 011,
    BankSelectLSB = 032,
    ModulationLSB = 033,
    BreathControllerLSB = 034,
    FootControllerLSB = 036,
    PortamentoTimeLSB = 037,
    VolumeLSB = 039,
    BalanceLSB = 040,
    PanLSB = 042,
    ExpressionLSB = 043,
    Sustain = 064,
    Portamento = 065,
    Sostenuto = 066,
    SoftPedal = 067,
    Legato = 068,
    Sustain2 = 069,
    PortamentoControl = 084,
    AllSoundOff = 120,
    ResetAllControllers = 121,
    LocalKeyboard = 122,
    AllNotesOff = 123,
}

-- Return the module.
return M

-- The GM midi instrument definitions.
-- instruments = {
-- "AcousticGrandPiano" = 000, "BrightAcousticPiano" = 001, "ElectricGrandPiano" = 002, "HonkyTonkPiano" = 003, "ElectricPiano1" = 004, "ElectricPiano2" = 005,
-- "Harpsichord" = 006, "Clavinet" = 007, "Celesta" = 008, "Glockenspiel" = 009, "MusicBox" = 010, "Vibraphone" = 011,
-- "Marimba" = 012, "Xylophone" = 013, "TubularBells" = 014, "Dulcimer" = 015, "DrawbarOrgan" = 016, "PercussiveOrgan" = 017,
-- "RockOrgan" = 018, "ChurchOrgan" = 019, "ReedOrgan" = 020, "Accordion" = 021, "Harmonica" = 022, "TangoAccordion" = 023,
-- "AcousticGuitarNylon" = 024, "AcousticGuitarSteel" = 025, "ElectricGuitarJazz" = 026, "ElectricGuitarClean" = 027, "ElectricGuitarMuted" = 028, "OverdrivenGuitar" = 029,
-- "DistortionGuitar" = 030, "GuitarHarmonics" = 031, "AcousticBass" = 032, "ElectricBassFinger" = 033, "ElectricBassPick" = 034, "FretlessBass" = 035,
-- "SlapBass1" = 036, "SlapBass2" = 037, "SynthBass1" = 038, "SynthBass2" = 039, "Violin" = 040, "Viola" = 041,
-- "Cello" = 042, "Contrabass" = 043, "TremoloStrings" = 044, "PizzicatoStrings" = 045, "OrchestralHarp" = 046, "Timpani" = 047,
-- "StringEnsemble1" = 048, "StringEnsemble2" = 049, "SynthStrings1" = 050, "SynthStrings2" = 051, "ChoirAahs" = 052, "VoiceOohs" = 053,
-- "SynthVoice" = 054, "OrchestraHit" = 055, "Trumpet" = 056, "Trombone" = 057, "Tuba" = 058, "MutedTrumpet" = 059,
-- "FrenchHorn" = 060, "BrassSection" = 061, "SynthBrass1" = 062, "SynthBrass2" = 063, "SopranoSax" = 064, "AltoSax" = 065,
-- "TenorSax" = 066, "BaritoneSax" = 067, "Oboe" = 068, "EnglishHorn" = 069, "Bassoon" = 070, "Clarinet" = 071,
-- "Piccolo" = 072, "Flute" = 073, "Recorder" = 074, "PanFlute" = 075, "BlownBottle" = 076,
-- "Shakuhachi" = 077, "Whistle" = 078, "Ocarina" = 079, "Lead1Square" = 080, "Lead2Sawtooth" = 081,
-- "Lead3Calliope" = 082, "Lead4Chiff" = 083, "Lead5Charang" = 084, "Lead6Voice" = 085, "Lead7Fifths" = 086, "Lead8BassAndLead" = 087,
-- "Pad1NewAge" = 088, "Pad2Warm" = 089, "Pad3Polysynth" = 090, "Pad4Choir" = 091, "Pad5Bowed" = 092, "Pad6Metallic" = 093,
-- "Pad7Halo" = 094, "Pad8Sweep" = 095, "Fx1Rain" = 096, "Fx2Soundtrack" = 097, "Fx3Crystal" = 098, "Fx4Atmosphere" = 099,
-- "Fx5Brightness" = 100, "Fx6Goblins" = 101, "Fx7Echoes" = 102, "Fx8SciFi" = 103, "Sitar" = 104, "Banjo" = 105,
-- "Shamisen" = 106, "Koto" = 107, "Kalimba" = 108, "BagPipe" = 109, "Fiddle" = 110, "Shanai" = 111,
-- "TinkleBell" = 112, "Agogo" = 113, "SteelDrums" = 114, "Woodblock" = 115, "TaikoDrum" = 116, "MelodicTom" = 117,
-- "SynthDrum" = 118, "ReverseCymbal" = 119, "GuitarFretNoise" = 120, "BreathNoise" = 121, "Seashore" = 122, "BirdTweet" = 123,
-- "TelephoneRing" = 124, "Helicopter" = 125, "Applause" = 126, "Gunshot" = 127 }

--[[
        -- The GM midi instrument definitions.
        _instruments <const> =
        {
            "AcousticGrandPiano", "BrightAcousticPiano", "ElectricGrandPiano", "HonkyTonkPiano", "ElectricPiano1", "ElectricPiano2", "Harpsichord",
            "Clavinet", "Celesta", "Glockenspiel", "MusicBox", "Vibraphone", "Marimba", "Xylophone", "TubularBells", "Dulcimer", "DrawbarOrgan",
            "PercussiveOrgan", "RockOrgan", "ChurchOrgan", "ReedOrgan", "Accordion", "Harmonica", "TangoAccordion", "AcousticGuitarNylon",
            "AcousticGuitarSteel", "ElectricGuitarJazz", "ElectricGuitarClean", "ElectricGuitarMuted", "OverdrivenGuitar", "DistortionGuitar",
            "GuitarHarmonics", "AcousticBass", "ElectricBassFinger", "ElectricBassPick", "FretlessBass", "SlapBass1", "SlapBass2", "SynthBass1",
            "SynthBass2", "Violin", "Viola", "Cello", "Contrabass", "TremoloStrings", "PizzicatoStrings", "OrchestralHarp", "Timpani",
            "StringEnsemble1", "StringEnsemble2", "SynthStrings1", "SynthStrings2", "ChoirAahs", "VoiceOohs", "SynthVoice", "OrchestraHit",
            "Trumpet", "Trombone", "Tuba", "MutedTrumpet", "FrenchHorn", "BrassSection", "SynthBrass1", "SynthBrass2", "SopranoSax", "AltoSax",
            "TenorSax", "BaritoneSax", "Oboe", "EnglishHorn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "PanFlute", "BlownBottle",
            "Shakuhachi", "Whistle", "Ocarina", "Lead1Square", "Lead2Sawtooth", "Lead3Calliope", "Lead4Chiff", "Lead5Charang", "Lead6Voice",
            "Lead7Fifths", "Lead8BassAndLead", "Pad1NewAge", "Pad2Warm", "Pad3Polysynth", "Pad4Choir", "Pad5Bowed", "Pad6Metallic", "Pad7Halo",
            "Pad8Sweep", "Fx1Rain", "Fx2Soundtrack", "Fx3Crystal", "Fx4Atmosphere", "Fx5Brightness", "Fx6Goblins", "Fx7Echoes", "Fx8SciFi",
            "Sitar", "Banjo", "Shamisen", "Koto", "Kalimba", "BagPipe", "Fiddle", "Shanai", "TinkleBell", "Agogo", "SteelDrums", "Woodblock",
            "TaikoDrum", "MelodicTom", "SynthDrum", "ReverseCymbal", "GuitarFretNoise", "BreathNoise", "Seashore", "BirdTweet", "TelephoneRing",
            "Helicopter", "Applause", "Gunshot"
        };
 
        /// <summary>The GM midi drum kit definitions.</summary>
        static readonly Dictionary<int, string> _drumKits = new()
        {
            { 0, "Standard" }, { 8, "Room" }, { 16, "Power" }, { 24, "Electronic" }, { 25, "TR808" },
            { 32, "Jazz" }, { 40, "Brush" }, { 48, "Orchestra" }, { 56, "SFX" }
        };
 
        /// <summary>The GM midi drum definitions.</summary>
        static readonly Dictionary<int, string> _drums = new()
        {
            { 035, "AcousticBassDrum" }, { 036, "BassDrum1" }, { 037, "SideStick" }, { 038, "AcousticSnare" }, { 039, "HandClap" }, 
            { 040, "ElectricSnare" }, { 041, "LowFloorTom" }, { 042, "ClosedHiHat" }, { 043, "HighFloorTom" }, { 044, "PedalHiHat" }, 
            { 045, "LowTom" }, { 046, "OpenHiHat" }, { 047, "LowMidTom" }, { 048, "HiMidTom" }, { 049, "CrashCymbal1" }, 
            { 050, "HighTom" }, { 051, "RideCymbal1" }, { 052, "ChineseCymbal" }, { 053, "RideBell" }, { 054, "Tambourine" }, 
            { 055, "SplashCymbal" }, { 056, "Cowbell" }, { 057, "CrashCymbal2" }, { 058, "Vibraslap" }, { 059, "RideCymbal2" }, 
            { 060, "HiBongo" }, { 061, "LowBongo" }, { 062, "MuteHiConga" }, { 063, "OpenHiConga" }, { 064, "LowConga" }, 
            { 065, "HighTimbale" }, { 066, "LowTimbale" }, { 067, "HighAgogo" }, { 068, "LowAgogo" }, { 069, "Cabasa" }, 
            { 070, "Maracas" }, { 071, "ShortWhistle" }, { 072, "LongWhistle" }, { 073, "ShortGuiro" }, { 074, "LongGuiro" }, 
            { 075, "Claves" }, { 076, "HiWoodBlock" }, { 077, "LowWoodBlock" }, { 078, "MuteCuica" }, { 079, "OpenCuica" }, 
            { 080, "MuteTriangle" }, { 081, "OpenTriangle" }
        };
 
        /// <summary>The midi controller definitions.</summary>
        static readonly Dictionary<int, string> _controllers = new()
        {
            { 000, "BankSelect" }, { 001, "Modulation" }, { 002, "BreathController" }, { 004, "FootController" }, { 005, "PortamentoTime" }, 
            { 007, "Volume" }, { 008, "Balance" }, { 010, "Pan" }, { 011, "Expression" }, { 032, "BankSelectLSB" }, { 033, "ModulationLSB" }, 
            { 034, "BreathControllerLSB" }, { 036, "FootControllerLSB" }, { 037, "PortamentoTimeLSB" }, { 039, "VolumeLSB" }, 
            { 040, "BalanceLSB" }, { 042, "PanLSB" }, { 043, "ExpressionLSB" }, { 064, "Sustain" }, { 065, "Portamento" }, { 066, "Sostenuto" }, 
            { 067, "SoftPedal" }, { 068, "Legato" }, { 069, "Sustain2" }, { 084, "PortamentoControl" }, { 120, "AllSoundOff" }, 
            { 121, "ResetAllControllers" }, { 122, "LocalKeyboard" }, { 123, "AllNotesOff" }
        };
        #endregion
]]--
