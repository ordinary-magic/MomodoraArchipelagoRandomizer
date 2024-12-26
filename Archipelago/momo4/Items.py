import typing

from BaseClasses import Item, ItemClassification, MultiWorld 
from typing import Dict
from .Options import Momo4Options

ITEM_BASE_CODE_OFFSET = 0x4400

class ItemData(typing.NamedTuple):
    code: int
    progression: bool = False
    quantity: int = 1

class Momo4Item(Item):
    game = "Momodora 4"

    def __init__(self, name, data, player: int = None):
        super(Momo4Item, self).__init__(
            name,
            ItemClassification.progression if data.progression else ItemClassification.useful,
            data.code,
            player
        )

# All items in the game, even the ones not in the multiworld
item_key: Dict[str, ItemData] = {
    'Adorned Ring': ItemData(1, quantity=0), # Easy mode starting item
    'Necklace Of Sacrifice': ItemData(2),
    'Bellflower': ItemData(4, quantity=5),
    'Astral Charm': ItemData(5),
    'Edea\'s Pearl': ItemData(6),
    'Dull Pearl': ItemData(7),
    'Red Ring': ItemData(8),
    'Magnet Stone': ItemData(9),
    'Rotten Bellflower': ItemData(10),
    'Faerie Tear': ItemData(11),
    'Impurity Flask': ItemData(13),
    'Passiflora': ItemData(14, quantity=2),
    'Crystal Seed': ItemData(15),
    'Medal Of Equivalence': ItemData(16, quantity=0), # Easy mode starting item
    'Tainted Missive': ItemData(17, quantity=2),
    'Black Sachet': ItemData(18),
    'Ring Of Candor': ItemData(21),
    'Small Coin': ItemData(22, quantity=0), # NG+ item
    'Bakman Patch': ItemData(23, progression=True), # Item both only exists in hard mode, and is progression for it
    'Cat Sphere': ItemData(24, progression=True),
    'Hazel Badge': ItemData(25, progression=True),
    'Torn Branch': ItemData(26),
    'Monastery Key': ItemData(27, progression=True),
    'Clarity Shard': ItemData(31),
    'Dirty Shroom': ItemData(32, progression=True),
    'Ivory Bug': ItemData(34, quantity=20, progression=True), # Any item that locks a location must be progression
    'Violet Sprite': ItemData(35),
    'Soft Tissue': ItemData(36, progression=True),
    'Garden Key': ItemData(37, progression=True),
    'Sparse Thread': ItemData(38),
    'Blessed Charm': ItemData(39),
    'Heavy Arrows': ItemData(40),
    'Bloodstained Tissue': ItemData(41),
    'Maple Leaf': ItemData(42, progression=True, quantity=0), # Randomizing this would be interesting, but its not detectable by the overlay (afaik)
    'Fresh Spring Leaf': ItemData(43, progression=True),
    'Pocket Incensory': ItemData(44, quantity=2),
    'Birthstone': ItemData(45, quantity=0), # NG+ item (final boss no-hit drop)
    'Quick Arrows': ItemData(46),
    'Drilling Arrows': ItemData(47),
    'Sealed Wind': ItemData(48, progression=True),
    'Cinder Key': ItemData(49, progression=True),
    'Crest Fragment - Bow Power': ItemData(50, progression=True),
    'Crest Fragment - Bow Speed': ItemData(51, progression=True),
    'Crest Fragment - Dash': ItemData(52, progression=True),
    'Crest Fragment - Warp': ItemData(53, progression=True),
    'Vitality Fragment': ItemData(54, quantity=17),
    "Final Boss Clear": ItemData(55, quantity=0, progression=True), # Should be an event item, but those are bugged in archipelago rn
    "All Items Obtained": ItemData(56, quantity=0, progression=True), # ^ (next ap release has the bugfix, and then these can be removed)
}

def get_all_items() -> Dict[str, ItemData]:
    '''Get a list of every single item in the game, regardless of options'''
    return item_key

def create_item(name: str, player: int) -> Momo4Item:
    '''Create a single item upon request by the server'''
    if name in item_key.keys():
        return Momo4Item(name, item_key[name], player)
    else: # Special "Event" items that aren't in the list and dont have a code
         return Momo4Item(name, ItemData(None, progression=True), player)


# Populte the world's item pool with the items from this game
def create_items(world: MultiWorld, player: int, options: Momo4Options):
    '''Preare a list of items that matches the input options'''
    all_items = [(x, item_key[x].quantity) for x in item_key.keys()]
    item_pool = []

    for item, amount in all_items:
        # Remove Vitality Fragments from the list if they are disallowed
        if item == 'Vitality Fragment' and not options.vitality:
            amount = 0
        
        # Remove Ivory Bugs and turn-in rewards if they are disallowed
        if not options.bugs:
            if item == 'Ivory Bug' or item == 'Blessed Charm' or item == 'Hazel Badge':
                amount = 0
            elif item == 'Bellflower' or item == 'Passiflora':
                amount -= 1

        # Remove Boss Drop Items if hard mode is off
        if not options.hard_mode:
            if item == 'Bakman Patch' or item == 'Sparse Thread' or item == 'Bloodstained Tissue' \
            or item == 'Edea\'s Pearl' or item == 'Torn Branch' or item == 'Heavy Arrows':
                amount = 0            
            elif item == 'Tainted Missive' or item == 'Pocket Incensory':
                amount -= 1

        # Add the item(s) to the pool
        item_pool += [create_item(item, player)] * amount
    world.itempool += item_pool
