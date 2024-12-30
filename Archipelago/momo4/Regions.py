from BaseClasses import Entrance, MultiWorld, Region
from .Locations import get_locations
from .Options import Momo4Options
from .Rules import get_region_locking_rule

def create_regions(world: MultiWorld, player: int, options: Momo4Options):
    # Setup the game's regions and their connections to one another
    world_map : dict[str, list[str]] = {
        'Cinder Chambers': [],
        'Forlorn Monastery': ['Cinder Chambers'],
        'Frore Ciele': [],
        'Karst Castle': [],
        'Karst City': ['Royal Pinacotheca', 'Subterranean Grave', 'Forlorn Monastery'],
        'Royal Pinacotheca': ['Karst Castle'],
        'Sacred Ordalia Grove': ['Frore Ciele', 'Karst City'],
        'Subterranean Grave': ['Whiteleaf Memorial Park'],
        'Whiteleaf Memorial Park': [],
        'Menu': ['Sacred Ordalia Grove']
    }
    # Note: this omits a few connections that would be redundant in the graph.

    # Create the regions and their exits
    regions = {}
    for name, exits in world_map.items():
        regions[name] = create_region(world, player, options, name, exits)
    
    # Connect the regions together
    for source in regions.values():
        for connection in world_map[source.name]:
            exit = Entrance(player, connection, source)

            rule = get_region_locking_rule(player, options, source.name, connection)
            if rule:
                exit.access_rule = rule
                
            source.exits.append(exit)
            exit.connect(regions[connection])

    # Add the regions to the multiworld
    world.regions += regions.values()

# Create a single region object with the specified parameters
def create_region(world: MultiWorld, player: int, options: Momo4Options, name: str, exits=[]):
    region = Region(name, player, world)
    region.locations = get_locations(options, name, region, player)
    return region