from BaseClasses import Entrance, MultiWorld, Region
from .Locations import get_locations
from .Options import Momo4Options
from .Rules import get_region_locking_rule

def create_regions(world: MultiWorld, player: int, options: Momo4Options):
    # Setup the game's regions and their connections to one another
    world_map : dict[str, list[str]] = {
        'Cinder Chambers': ['Forlorn Monastery'],
        'Forlorn Monastery': [],
        'Frore Ciele': [],
        'Karst Castle': [],
        'Karst City': ['Royal Pinacotheca', 'Subterranean Grave'],
        'Royal Pinacotheca': ['Karst Castle'],
        'Sacred Ordalia Grove': ['Frore Ciele', 'Karst City'],
        'Subterranean Grave': ['Whiteleaf Memorial Park'],
        'Whiteleaf Memorial Park': ['Cinder Chambers'],
        'Menu': ['Sacred Ordalia Grove']
    }
    # Note: This omits a lot of connections within the game world, but generally tracks a player's progression instead
    #  If there are early drops avaliable in locked regions this might break progression but i can evaluate that later (TODO)

    # Create the regions and their exits
    regions = {}
    for name, exits in world_map.items():
        regions[name] = create_region(world, player, options, name, exits)

    # Connect the exits to other regions
    for region in regions.values():
        for exit in region.exits:
            rule = get_region_locking_rule(player, options, region.name, exit.name)
            exit.connect(regions[exit.name], rule)

    # Add the regions to the multiworld
    world.regions += regions.values()

# Create a single region object with the specified parameters
def create_region(world: MultiWorld, player: int, options: Momo4Options, name: str, exits=[]):
    region = Region(name, player, world)
    region.locations = get_locations(options, name, region, player)

    # Setup region exits
    for exit in exits:
        entrance = Entrance(player, exit, region)
        region.exits.append(entrance)

    return region