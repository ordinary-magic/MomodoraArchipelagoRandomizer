from BaseClasses import CollectionState, MultiWorld
from ..generic.Rules import add_item_rule, set_rule
from .Items import Momo4Item, create_item
from .Locations import is_valid_location_name
from .Options import Goal, Momo4Options

def set_rules(world: MultiWorld, player: int, options: Momo4Options):
    '''Setup the item placement rules for Momodora 4'''

    ''' The original randomizer's requirement lists (for reference):
    requiresCatSphere = new List<int> { 24, 27, 39, 47, 48, 55, 63, 64, 65, 66, 67, 68, 70, 74, 75, 79 };
    requiresCrestFragments = new List<int> { 0, 2, 17, 18, 19, 20, 21, 22, 23, 38, 39, 47, 50, 51, 52, 53, 54, 55, 71, 72, 73, 74, 75 };
    requiresGardenKey = new List<int> { 66, 67, 68, 35, 26, 25, 13 };
    requiresCinderKey = new List<int> { 49 };
    requiresMonasteryKey = new List<int> { 27, 36, 69, 70, 78 };
    requiresHazelBadge = new List<int> { 29 };
    requiresSoftTissue = new List<int> { 37 };
    requiresDirtyShroom = new List<int> { 30 };
    requiresSealedWind = new List<int> { 28 };
    requiresIvoryBugs = new List<int> {80, 81, 82};
    '''

    ### Cat Sphere ###
    # Note: Frore Ciele locations are locked by the region access rule
    reqs_cat_sphere = [
        #'Dirty Shroom', # Not Randomized due to bug
        'Imp - Dirty Shroom Trade', # Replacement check
        'Soft Tissue',
        'Vitality Fragment 1 - Sacred Ordalia Grove',
        'Vitality Fragment 3 - Sacred Ordalia Grove',
        'Vitality Fragment 3 - Karst Castle',
        'Ivory Bug 3 - Sacred Ordalia Grove',
        'Ivory Bug - Subterranean Grave',
        'Ivory Bug 1 - Whiteleaf Memorial Park',
        'Ivory Bug 2 - Whiteleaf Memorial Park',
        'Ivory Bug 3 - Whiteleaf Memorial Park',
        'Ivory Bug 2 - Forlorn Monastery',
        'Ivory Bug 2 - Royal Pinacotheca',
        'Ivory Bug 3 - Royal Pinacotheca',
    ]
    for location in reqs_cat_sphere:
        if is_valid_location_name(location, options):
            set_rule(world.get_location(location, player), lambda state: state.has("Cat Sphere", player))


    ### Crest Fragments ###
    # Anything in Karst Castle / Royal Pinacotheca are handled by the region lock
    reqs_dash_fragment = [
        # TODO: Bellflower location dependencies seem wrong, need to check these
        'Bellflower - Karst City', # Castle + one other, but idk which
        'Vitality Fragment 1 - Sacred Ordalia Grove',
        'Vitality Fragment 3 - Sacred Ordalia Grove',
    ]
    for location in reqs_dash_fragment:
        if is_valid_location_name(location, options):
            set_rule(world.get_location(location, player), lambda state: state.has("Crest Fragment - Dash", player))

    ### Garden Key ###
    # Garden Key dependencies are handled by the region lock  

    ### Cinder Key ###
    if options.vitality:
        set_rule(world.get_location('Vitality Fragment 2 - Cinder Chambers', player), lambda state: state.has('Cinder Key', player))

    ### Monastery Key ###
    reqs_monastery_key = [
        'Soft Tissue',
        'Boss - Pardoner Fennel',
        'Ivory Bug 1 - Forlorn Monastery',
        'Ivory Bug 2 - Forlorn Monastery',
        'Crest Fragment - Forlorn Monastery'
    ]
    for location in reqs_monastery_key:
        if is_valid_location_name(location, options):
            set_rule(world.get_location(location, player), lambda state: state.has("Monastery Key", player))

    ### Soft Tissue ###
    if options.hard_mode:
        set_rule(world.get_location('Boss - Archpriestess Choir', player), lambda state: state.has("Soft Tissue", player))

    ### Dirty Shroom ###
    # Not currently randomized because getting it early removes the item from the map
    #set_rule(world.get_location('Imp - Dirty Shroom Trade', player), lambda state: state.has('Dirty Shroom', player))

    ### Sealed Wind ###
    set_rule(world.get_location("Fresh Spring Leaf", player), lambda state: state.has("Sealed Wind", player))

    ### Ivory Bugs (and related items) ###
    if options.bugs:
        # Ensure logic knows how many bugs the rewards cost
        set_rule(world.get_location('10 Ivory Bug Reward', player), lambda state: state.has("Ivory Bug", player, 10))
        set_rule(world.get_location('15 Ivory Bug Reward', player), lambda state: state.has("Ivory Bug", player, 15))
        set_rule(world.get_location('20 Ivory Bug Reward', player), lambda state: state.has("Ivory Bug", player, 20))

        # And the bonus badge trade step
        set_rule(world.get_location('Eri - Hazel Badge', player), lambda state: state.has("Hazel Badge", player))

    ### Victory Conditions ###
    world.get_location('Final Boss - Accurst Queen of Karst', player).place_locked_item(create_item('Final Boss Clear', player))    
    world.get_location('All Items Obtained', player).place_locked_item(create_item('All Items Obtained', player))

    if options.goal is Goal.option_100: # Need to check items and boss clear
        world.completion_condition[player] = lambda state: state.has("Final Boss Clear", player) and state.has("All Items Obtained", player)
    else: # Boss clear condition checks for any% vs true ending conditions on the client
        world.completion_condition[player] = lambda state: state.has("Final Boss Clear", player)

### Region Locks ###
# Note: the wrappers are needed because afaik we need to be able to reference the player id to check items
def get_cat_sphere_rule(player: int, options: Momo4Options):
    '''Get a rule function to check if we have the cat sphere'''
    
    def rule(state: CollectionState) -> bool:
        return state.has("Cat Shpere", player)
    return rule

def get_crest_fragment_rule(player: int, options: Momo4Options):
    '''Get a rule function to check if four crest fragments have been unlocked'''

    def rule(state: CollectionState) -> bool:
        return state.has('Crest Fragment - Bow Power', player) \
        and state.has('Crest Fragment - Bow Speed', player) \
        and state.has('Crest Fragment - Dash', player) \
        and state.has('Crest Fragment - Warp', player)
    # Technically, you can clip this door but thats likely *too* hard
    return rule

def get_garden_key_rule(player: int, options: Momo4Options):
    '''Get a rule function to check if we can access the garden'''
    
    def rule(state: CollectionState) -> bool:
        return state.has("Garden Key", player) or \
            (options.hard_mode and state.has("Bakman Patch", player)) 
    return rule   

region_lock_rules = [
    ('Sacred Ordalia Grove', 'Frore Ciele', get_cat_sphere_rule),
    ('Subterranean Grave', 'Whiteleaf Memorial Park', get_garden_key_rule),
    ('Karst City', 'Royal Pinacotheca', get_crest_fragment_rule)
]

def get_region_locking_rule(player, options, region_from, region_to):
    '''Get a rule about what is needed to progress from one region to another
       if such a rule exists for the provided pair of regions.'''
    for rule_from, rule_to, rule in region_lock_rules:
        if rule_from == region_from and rule_to == region_to:
            return rule(player, options)

    # If no explicit rule is found, progress is always allowed
    return lambda _: True