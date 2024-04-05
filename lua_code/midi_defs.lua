
-- Create the namespace/module.
local M = {}

-- The GM midi instrument definitions.
M.instruments =
{
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
    TelephoneRing = 124, Helicopter = 125, Applause = 126, Gunshot = 127
}

-- The GM midi drum definitions.
M.drums =
{
    AcousticBassDrum = 035, BassDrum1 = 036, SideStick = 037, AcousticSnare = 038, HandClap = 039, ElectricSnare = 040,
    LowFloorTom = 041, ClosedHiHat = 042, HighFloorTom = 043, PedalHiHat = 044, LowTom = 045, OpenHiHat = 046,
    LowMidTom = 047, HiMidTom = 048, CrashCymbal1 = 049, HighTom = 050, RideCymbal1 = 051, ChineseCymbal = 052,
    RideBell = 053, Tambourine = 054, SplashCymbal = 055, Cowbell = 056, CrashCymbal2 = 057, Vibraslap = 058,
    RideCymbal2 = 059, HiBongo = 060, LowBongo = 061, MuteHiConga = 062, OpenHiConga = 063, LowConga = 064,
    HighTimbale = 065, LowTimbale = 066, HighAgogo = 067, LowAgogo = 068, Cabasa = 069, Maracas = 070, ShortWhistle = 071,
    LongWhistle = 072, ShortGuiro = 073, LongGuiro = 074, Claves = 075, HiWoodBlock = 076, LowWoodBlock = 077,
    MuteCuica = 078, OpenCuica = 079, MuteTriangle = 080, OpenTriangle = 081,
}

-- The GM midi drum kit definitions.
M.drum_kits =
{
    Standard = 0, Room = 8, Power = 16, Electronic = 24, TR808 = 25, Jazz = 32, Brush = 40, Orchestra = 48, SFX = 56
}

-- The midi controller definitions.
M.controllers =
{
    BankSelect = 000, Modulation = 001, BreathController = 002, FootController = 004, PortamentoTime = 005, Volume = 007,
    Balance = 008, Pan = 010, Expression = 011, BankSelectLSB = 032, ModulationLSB = 033, BreathControllerLSB = 034,
    FootControllerLSB = 036, PortamentoTimeLSB = 037, VolumeLSB = 039, BalanceLSB = 040, PanLSB = 042, ExpressionLSB = 043,
    Sustain = 064, Portamento = 065, Sostenuto = 066, SoftPedal = 067, Legato = 068, Sustain2 = 069,
    PortamentoControl = 084, AllSoundOff = 120, ResetAllControllers = 121, LocalKeyboard = 122, AllNotesOff = 123,
}

--- Make markdown content from the definitions. TODOF
-- @return list of strings
-- function M.format_doc()

-- Return the module.
return M
