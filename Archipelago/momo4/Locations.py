from typing import Dict, NamedTuple
from BaseClasses import Location, Region
from .Items import BASE_OFFSET
from .Options import Momo4Options

class LocationData(NamedTuple):
    code: int
    boss: bool = False
    vitality_fragment: bool = False
    ivory_bug: bool = False

class Momo4Location(Location):
    game: str = "Momodora 4"
    data: LocationData

    def __init__(self, name, data, player, region):
        super().__init__(player, name, data.code + BASE_OFFSET, region)
        self.data = data


locations_by_region = {
    'Sacred Ordalia Grove': {
        'Bellflower - Sacred Ordalia Grove': LocationData(0),
        'Astral Charm': LocationData(1),
        'Eri - Hazel Badge': LocationData(29, ivory_bug=True),
        'Imp - Dirty Shroom Trade': LocationData(30),
        'Boss - Anthropod Demon Edea': LocationData(31, boss=True),
        'Vitality Fragment 1 - Sacred Ordalia Grove': LocationData(39, vitality_fragment=True),
        'Vitality Fragment 2 - Sacred Ordalia Grove': LocationData(40, vitality_fragment=True),
        'Vitality Fragment 3 - Sacred Ordalia Grove': LocationData(47, vitality_fragment=True),
        'Ivory Bug 1 - Sacred Ordalia Grove': LocationData(56, ivory_bug=True),
        'Ivory Bug 2 - Sacred Ordalia Grove': LocationData(57, ivory_bug=True),
        'Ivory Bug 3 - Sacred Ordalia Grove': LocationData(63, ivory_bug=True),
    },

    'Karst City': {
        'Bellflower - Karst City': LocationData(2),
        'Magnet Stone': LocationData(3),
        'Shop - Crystal Seed': LocationData(8),
        'Shop - Faerie Tear': LocationData(9),
        'Shop - Ring of Candor': LocationData(10),
        'Fresh Spring Leaf': LocationData(28),
        'Boss -  Lubella, the Witch of Decay 1': LocationData(32, boss=True),
        'Vitality Fragment 1 - Karst City': LocationData(41, vitality_fragment=True),
        'Vitality Fragment 2 - Karst City': LocationData(42, vitality_fragment=True),
        'Ivory Bug 1 - Karst City': LocationData(58, ivory_bug=True),
        'Ivory Bug 2 - Karst City': LocationData(59, ivory_bug=True),
    },

    'Subterranean Grave': {
        'Garden Key': LocationData(4),
        'Shop - Clarity Shard': LocationData(11),
        'Shop - Necklace of Sacrifice': LocationData(12),
        #'Dirty Shroom': LocationData(24), # Excluded due to bug
        'Boss - Derelict Frida': LocationData(33, boss=True),
        'Vitality Fragment - Subterranean Grave': LocationData(43, vitality_fragment=True),
        'Ivory Bug - Subterranean Grave': LocationData(65, ivory_bug=True),
        'Crest Fragment - Subterranean Grave': LocationData(76),
    },

    'Forlorn Monastery': {
        'Monastery Key': LocationData(6),
        'Tainted Missive': LocationData(7),
        'Soft Tissue': LocationData(27),
        'Boss - Pardoner Fennel': LocationData(36, boss=True),
        'Boss - Archpriestess Choir': LocationData(37, boss=True),
        'Vitality Fragment - Forlorn Monastery': LocationData(46, vitality_fragment=True),
        'Ivory Bug 1 - Forlorn Monastery': LocationData(69, ivory_bug=True),
        'Ivory Bug 2 - Forlorn Monastery': LocationData(70, ivory_bug=True),
        'Crest Fragment - Forlorn Monastery': LocationData(78),
    },

    'Cinder Chambers': {
        'Cinder Key': LocationData(5),
        'Shop - Red Ring': LocationData(14),
        'Spider Shop - Drilling Arrows': LocationData(15),
        'Spider Shop - Impurity Flask': LocationData(16),
        'Boss - Cinder Chambers': LocationData(34, boss=True),
        'Vitality Fragment 1 - Cinder Chambers': LocationData(45, vitality_fragment=True),
        'Vitality Fragment 2 - Cinder Chambers': LocationData(49, vitality_fragment=True),
        'Ivory Bug 1 - Cinder Chambers': LocationData(60, ivory_bug=True),
        'Ivory Bug 2 - Cinder Chambers': LocationData(61, ivory_bug=True),
        'Ivory Bug 3 - Cinder Chambers': LocationData(62, ivory_bug=True),
        'Crest Fragment - Cinder Chambers': LocationData(77),
    },

    'Frore Ciele': {
        'Crest Fragment - Frore Ciele': LocationData(79),
        'Vitality Fragment - Frore Ciele': LocationData(48, vitality_fragment=True),
        'Ivory Bug - Frore Ciele': LocationData(64, ivory_bug=True),
    },

    'Whiteleaf Memorial Park': {
        'Shop - Dull Pearl': LocationData(13),
        'Bellflower - Whiteleaf Memorial Park': LocationData(26),
        'Cat Sphere': LocationData(25),
        'Boss - Lubella, the Witch of Decay 2': LocationData(35, boss=True),
        'Vitality Fragment - Whiteleaf Memorial Park': LocationData(44, vitality_fragment=True),
        'Ivory Bug 1 - Whiteleaf Memorial Park': LocationData(66, ivory_bug=True),
        'Ivory Bug 2 - Whiteleaf Memorial Park': LocationData(67, ivory_bug=True),
        'Ivory Bug 3 - Whiteleaf Memorial Park': LocationData(68, ivory_bug=True),
        '10 Ivory Bug Reward': LocationData(80, ivory_bug=True),
        '15 Ivory Bug Reward': LocationData(81, ivory_bug=True),
        '20 Ivory Bug Reward': LocationData(82, ivory_bug=True),
    },

    'Royal Pinacotheca': {
        'Shop - Violet Sprite': LocationData(17),
        'Shop - Quick Arrows': LocationData(18),
        'Shop - Pocket Incensory': LocationData(19),
        'Black Sachet': LocationData(20),
        'Sealed Wind': LocationData(21),
        'Boss - Duquess Lupair and Magnolia': LocationData(38, boss=True),
        'Vitality Fragment 1 - Royal Pinacotheca': LocationData(50, vitality_fragment=True),
        'Vitality Fragment 2 - Royal Pinacotheca': LocationData(51, vitality_fragment=True),
        'Vitality Fragment 3 - Royal Pinacotheca': LocationData(52, vitality_fragment=True),
        'Ivory Bug 1 - Royal Pinacotheca': LocationData(71, ivory_bug=True),
        'Ivory Bug 2 - Royal Pinacotheca': LocationData(74, ivory_bug=True),
        'Ivory Bug 3 - Royal Pinacotheca': LocationData(75, ivory_bug=True),
    },

    'Karst Castle': {
        'Cath - Karst Castle': LocationData(23), 
        'Bellflower - Karst Castle': LocationData(22),
        'Vitality Fragment 1 - Karst Castle': LocationData(53, vitality_fragment=True),
        'Vitality Fragment 2 - Karst Castle': LocationData(54, vitality_fragment=True),
        'Vitality Fragment 3 - Karst Castle': LocationData(55, vitality_fragment=True),
        'Ivory Bug 1 - Karst Castle': LocationData(72, ivory_bug=True),
        'Ivory Bug 2 - Karst Castle': LocationData(73, ivory_bug=True),
        'Final Boss - Accurst Queen of Karst': LocationData(83), # Must be last location
    },

    'Menu' : {
    }
}

def get_all_locations() -> Dict[str, LocationData]:
    '''Get a top level list of all possible locations in the game'''
    result = {}
    for region in locations_by_region.values():
        result.update(region)
    return result

def get_locations(options: Momo4Options, name: str, region: Region, player: int) -> list[Momo4Location]:
    '''Prepare a list of properly formatted archipelago Location objects for the corresponding region'''

    def check_options(location_pair):
        location = location_pair[1]
        return (options.bugs or not location.ivory_bug) and \
            (options.vitality or not location.vitality_fragment) and \
            (options.hard_mode or not location.boss)
    
    def make_location(location_pair):
        return Momo4Location(location_pair[0], location_pair[1], player, region)

    return [make_location(x) for x in locations_by_region[name].items() if check_options(x)]

def is_valid_location_name(name: str, options: Momo4Options) -> bool:
    '''Check if the supplied location name is valid under the current option set'''
    return is_valid_location(get_all_locations()[name], options)

def is_valid_location(location: LocationData, options: Momo4Options) -> bool:
    '''Check if the supplied location data is valid under the current option set'''
    return (options.bugs or not location.ivory_bug) and \
            (options.vitality or not location.vitality_fragment) and \
            (options.hard_mode or not location.boss)