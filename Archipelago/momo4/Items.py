import typing

from BaseClasses import Item, ItemClassification, MultiWorld  # type: ignore
from typing import Dict
from math import ceil, floor
from .Settings import Momo4Options

# All id's need to be unique across all games, thus, we define an starting offset value
BASE_OFFSET = 0x44400

class ItemData(typing.NamedTuple):
    code: int
    progression: bool = False
    trap: bool = False
    useful: bool = False # Items which are *especailly* useful to collect
    quantity: int = 1

class Momo4Item(Item):
    game = "Momodora 4"

    def __init__(self, name: str, data: ItemData, player: int):
        # Determine the item classification
        # "Trap" is a mutually exclusive type
        if (data.trap):
            type = ItemClassification.trap
        else:

            # Otherwise, start with filler (0) and add the correct bits to it
            type = ItemClassification.filler

            if (data.progression):
                type |= ItemClassification.progression

            if (data.useful) :
                type |= ItemClassification.useful
        
        
        super(Momo4Item, self).__init__(name, type, data.code + BASE_OFFSET, player)

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
    'Cat Sphere': ItemData(24, progression=True, useful=True),
    'Hazel Badge': ItemData(25, progression=True),
    'Torn Branch': ItemData(26),
    'Monastery Key': ItemData(27, progression=True),
    'Clarity Shard': ItemData(31),
    'Dirty Shroom': ItemData(32, progression=True, quantity=0), # Excluded from randomization due to bug
    'Ivory Bug': ItemData(34, quantity=20, progression=True), # Any item that locks a location must be progression
    'Violet Sprite': ItemData(35),
    'Soft Tissue': ItemData(36, progression=True),
    'Garden Key': ItemData(37, progression=True),
    'Sparse Thread': ItemData(38),
    'Blessing Charm': ItemData(39),
    'Heavy Arrows': ItemData(40),
    'Bloodstained Tissue': ItemData(41),
    'Maple Leaf': ItemData(42, progression=True, quantity=0), # Randomizing this would be interesting, but its not detectable by the overlay (afaik)
    'Fresh Spring Leaf': ItemData(43, progression=True, useful=True),
    'Pocket Incensory': ItemData(44), # Despite being in two places, you can only have one.
    'Birthstone': ItemData(45, quantity=0), # NG+ item (final boss no-hit drop)
    'Quick Arrows': ItemData(46),
    'Drilling Arrows': ItemData(47),
    'Sealed Wind': ItemData(48, progression=True),
    'Cinder Key': ItemData(49, progression=True),
    'Crest Fragment - Bow Power': ItemData(50, progression=True, useful=True),
    'Crest Fragment - Bow Speed': ItemData(51, progression=True, useful=True),
    'Crest Fragment - Dash': ItemData(52, progression=True, useful=True),
    'Crest Fragment - Warp': ItemData(53, progression=True, useful=True),
    'Vitality Fragment': ItemData(54, quantity=17),

    # "Items" added by archipelago
    "Final Boss Clear": ItemData(55, quantity=0, progression=True), # Special Archipelago Event
    "1 Munny": ItemData(56, quantity=0),
    "10 Munny": ItemData(57, quantity=0),
    "50 Munny": ItemData(58, quantity=0),
    "100 Munny": ItemData(59, quantity=0),
    "250 Munny": ItemData(60, quantity=0),
    "500 Munny": ItemData(61, quantity=0),
    "999 Munny": ItemData(62, quantity=0),
    "Ivory Swarm": ItemData(63, quantity=0, progression=True),
    "Vitality Chunk": ItemData(64, quantity=0),
}

def get_all_items() -> Dict[str, ItemData]:
    '''Get a list of every single item in the game, regardless of options'''
    return item_key

def create_item(name: str, player: int) -> Momo4Item:
    '''Create a single item upon request by the server'''
    if name in item_key.keys():
        return Momo4Item(name, item_key[name], player)
    else: # Not a real item, make it an "event" item instead
         return Momo4Item(name, ItemData(-1, progression=True), player)
    
# Name/Amount of every required item in the generation
RequiredItemCounts: list[tuple[str, int]] = []

# Populte the world's item pool with the items from this game
def create_items(world: MultiWorld, player: int, options: Momo4Options):
    '''Preare a list of items that matches the input options'''
    all_items = [(x, item_key[x].quantity) for x in item_key.keys()]
    item_pool = []

    for item, amount in all_items:
        # Remove Vitality Fragments from the list if they are disallowed or stacked
        if item == 'Vitality Fragment' and (options.stacking or not options.vitality):
            amount = 0

        # Add the vitality chunk stack item
        if item == 'Vitality Chunk' and options.vitality and options.stacking:
            amount = 4
        
        # Remove Ivory Bugs and turn-in rewards if they are disallowed
        if not options.bugs:
            if item == 'Ivory Bug' or item == 'Blessing Charm' or item == 'Hazel Badge':
                amount = 0
            elif item == 'Bellflower' or item == 'Passiflora':
                amount -= 1

        # Replace ivory bugs with ivory swarm if stacking is enabled
        elif options.bugs and options.stacking:
            if item == "Ivory Swarm":
                amount = 4
            elif item == "Ivory Bug":
                amount = 0

        # Remove Boss Drop Items if hard mode is off
        if not options.hard_mode:
            if item == 'Bakman Patch' or item == 'Sparse Thread' or item == 'Bloodstained Tissue' \
            or item == 'Edea\'s Pearl' or item == 'Torn Branch' or item == 'Heavy Arrows':
                amount = 0

            # Techinically, should always have 1 incensory, but the shop version misbehaves too much
            elif item == 'Tainted Missive' or item == 'Pocket Incensory':
                amount -= 1

        # Make the item
        item_obj = create_item(item, player)

        # If this is 100% mode, every (non-filler) item is required, so give it the progression tag
        if options.goal.value == 2:
            item_obj.classification |= ItemClassification.progression

        # Add it to the pool
        RequiredItemCounts.append((item, amount))
        item_pool += [item_obj] * amount

    # Create and add filler items if needed
    filler_amount = count_filler(options)
    if filler_amount > 0:
        world.itempool += create_filler(filler_amount, world, player, options)
    
    world.itempool += item_pool

def count_filler(options: Momo4Options) -> int:
    '''Count how many filler items will be present in this generation'''
    
    filler_amount = 0

    if options.stacking and options.bugs:
        filler_amount += 16 # bugs are condensed from 20 drops into 4 drops of 5

    if options.stacking and options.vitality:
        filler_amount += 13 # v fragments are condensed from 17 drops into 4 drops of 4-5

    return filler_amount

def create_filler(amount: int, world: MultiWorld, player: int, options: Momo4Options) -> list[Momo4Item]:
    '''Create an amount of filler items'''
    filler = []

    # Currently, we only check for munny, so if we must make filler and munny = none, just return a bunch of 1 munny items
    if options.munny == 0:
        return [create_item("1 Munny", player)] * amount

    # Otherwise, determine how many of each part of the range to award
    best_amount = 1
    worst_amount = 1 if amount > 1 else 0
    high_amount = floor(amount / 5)
    remaining = amount - best_amount - high_amount - worst_amount
    low_amount = ceil(remaining / 2)
    mid_amount = floor(remaining / 2)
    amounts = [worst_amount, low_amount, mid_amount, high_amount, best_amount]

    # Determine the range of munny
    if options.munny == 1: # cheap
        items = ["1 Munny", "1 Munny", "10 Munny", "50 Munny", "100 Munny"]
    elif options.munny == 2: # standard
        items = ["1 Munny", "50 Munny", "100 Munny", "250 Munny", "500 Munny"]
    else: # rich
        items = ["1 Munny", "100 Munny", "250 Munny", "500 Munny", "999 Munny"]

    # Turn the two lists into a set of items
    for i in range(0, len(items)):
        filler += [create_item(items[i], player)] * amounts[i]

    # Return the filler items
    return filler

