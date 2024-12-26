from .Items import create_item, create_items, get_all_items, Momo4Item
from .Locations import get_all_locations
from .Options import make_slot_data, Momo4Options
from .Regions import create_regions
from .Rules import set_rules
from ..AutoWorld import WebWorld, World
from BaseClasses import Tutorial

class Momo4WebWorld(WebWorld):
    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to playing Momodora 4: Reverie Under the Moonlight with Archipelago.",
        "English",
        "setup_en.md",
        "setup/en",
        ["ordinary_magic"]
    )
    theme = 'jungle'
    tutorials = [setup_en]

class Momo4World(World):
    """
    Momodora 4 - Reverie Under the Moonlight
    """
    options_dataclass = Momo4Options
    options: Momo4Options
    game = "Momodora 4 - Reverie Under the Moonlight"
    settings: None
    topology_present = False
    required_client_version = (0, 3, 7)
    web = Momo4WebWorld()

    # Required id resolution dicts for the superclass
    item_name_to_id = {name: data.code for name, data in get_all_items().items()}
    location_name_to_id = {name: data.code for name, data in get_all_locations().items()}

    def create_item(self, name: str) -> Momo4Item:
        create_item(name, self.player)

    def create_items(self):
        create_items(self.multiworld, self.player, self.options)

    def create_regions(self):
        create_regions(self.multiworld, self.player, self.options)

    def set_rules(self):
        set_rules(self.multiworld, self.player, self.options)

    def fill_slot_data(self) -> dict:
        return make_slot_data(self.options)
