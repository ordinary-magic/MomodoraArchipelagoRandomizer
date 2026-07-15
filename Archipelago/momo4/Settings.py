from dataclasses import dataclass

from Options import Choice, Toggle, Range, PerGameCommonOptions # type: ignore

class Goal(Choice):
    """Choose what the victory condition of your run is:
    'Any%' - You must defeat the Queen of Karst Castle
    'True Ending' - You must get game's True Ending
    '100%' - You must colelct every single randomized location and then beat the final boss
    """
    display_name = "Victory Condition"
    option_any = 0
    option_true = 1
    option_100 = 2

class FastKill(Choice):
    """If this is enabled, any damage taken will instantly kill you, regardless of hp.
    Can be on/off all the time or else only enabled during boss fights."""
    display_name = "Fast Kill"
    option_never = 0
    option_boss = 1
    option_always = 2

class IvoryBugs(Toggle):
    """If this is enabled, Ivory Bug locations will be included as checks."""
    display_name = "Include Ivory Bugs"

class VitalityFragments(Toggle):
    """If this is enabled, Vitality Fragment locations will be included as checks."""
    display_name = "Include Vitality Fragments"

class Stacking(Toggle):
    """If this is enabled, Ivory Bug and Vitality Fragment drops will be grouped so that you get multiple at once,
       and their spots in the pool will be replaced with new items instead."""
    display_name = "Item Stacking"

class Munny(Choice):
    """If this is enabled, should we include Munny in the filler items, and how much munny should we drop?
       If set to none, this will be banned by the filler algorightm unless we run out of other items
       If set to standard, we will drop several 50 and 100 munny drops, some 250 drops, and one 500 drop.
       Broke and Rich keep the same distribution, but increase or decrease the amount dropped at each tier."""
    display_name = "Munny"
    option_none = 0
    option_broke = 1
    option_standard = 2
    option_rich = 3
    default = 2

class HardMode(Toggle):
    """Makes item logic less strict. You will have to do speedrun tech to progress.
    Specifically, no-hit boss drops and garden key skip are in logic."""
    display_name = "HARD MODE"

class DeathLink(Choice):
    """If enabled, dying will kill all other deathlink players, and vice versa.
    Can be on/off all the time or else only enabled during boss fights."""
    display_name = "Death Link"
    option_never = 0
    option_boss = 1
    option_always = 2
    
class DeathLinkAmount(Range):
    """How many deaths will it take to trigger a deathlink. (Ignored if Deathlink=never)"""
    display_name = "Death Link Amount"
    range_start = 1
    range_end = 100
    default = 1

@dataclass
class Momo4Options(PerGameCommonOptions):
    goal: Goal
    fast_kill: FastKill
    bugs: IvoryBugs
    vitality: VitalityFragments
    stacking: Stacking
    munny: Munny
    hard_mode: HardMode
    deathlink: DeathLink
    dl_amount: DeathLinkAmount

def make_option_slot_data(options: Momo4Options):
    '''Extract and format relevant slot data for a set of options'''
    return {
        'goal': options.goal.value,
        'fast_kill': options.fast_kill.value,
        'deathlink': options.deathlink.value,
        'dl_amount': options.dl_amount.value,
        'hard_mode': options.hard_mode.value,
    }