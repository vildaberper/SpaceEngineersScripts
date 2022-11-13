# OmniScript

Successor of AutoInv with a focus on reliability and automation. Spend less time renaming stuff and watch it do what you want out-of-the-box.

## Usage

Every type of block with an inventory is configurable in its name and/or CustomData through filters. If a filter is defined for the block, OS will try to pull those items to it. The filter is configured based on what items the inventory can accept, what items are defined, the priority and quota. Defined items, priority and quota are all optional parameters, but at least one has to be supplied.

### Priority and Quota

Priority is probably pretty self-explanatory. Higher priority means that inventory will get filled first. If no priority is given, it will default to `p0`. If two inventories have the same priority, the one with a quota will get filled first. If both have a quota set, the one with the lowest quota will get filled first.

Quota means that OS will try to put an amount of items in the inventory. A quota of `q100` means that only 100 of that item will get put in it. The default quota is unlimited, or rather only restricted by inventory space.

#### Priority examples

Items will be put in A until the quota of `q1` is met. Then B until `q2` is met. And then C until the inventory is full. And so on...

A:`p2 q1` > B:`p2 q2` > C:`p2` > D:`q10` > E:`q100` > F:`p-1`

### Defining a filter

For example: If we want to make sure a reactor always has some uranium in it, the filter could look like this: `(ingot/uranium p100 q100 os)`

Since the reactor can only accept uranium in its inventory, we can reduce it to: `(* p100 q100 os)`

And if the item specification is omitted, it defaults to all accepted items: `(p100 q100 os)`

Which is the default filter for Large Reactors. This can of course be changed inside the script (configuration is in the top). Any block with with a filter defined will no longer use its default filter. You can also exclude the block entirely from the system by adding a `!` anywhere in the name, for example: `Large Reactor!`

### Multiple filters

Multiple filters can be applied to the same block by separating them with `;`: `(ingot p10;ice p1 q100 os)`

You can also define multiple filters in CustomData with the following format:

```
[os]
filter=
|ingot p10
|ice p1 q100
```

### Defining items

 - Any item can be defined by its name, for example: `solarcell`, or just the beginning of it: `solar`
 - Multiple items can be defined by the group, for example: `component`, or the beginning of it: `compo`
 - Items with ambiguous names can be specified with both its group and name: `ore/stone`
 - Multiple items can be specified in the same filter: `(ice ingot os)`
 - Items can also be excluded from the filter by adding `-` in front of it: `(ore -ice os)`
 - The specification is based on what items the inventory can accept. This means that the item `iron` will mean just the ore if the block is a Refinery, but both the ore and ingot if the block is a Cargo Container. For the Cargo Container, you also have to specify the group: `ingot/iron`

### Examples
 - A storage cargo with everything except tools and ice in it: `(* -tool -ice p-100 os)`
 - A Refinery to process every ore except platinum: `(* -plat p100 q1000 os)`
 - An access point cargo with components in it: `(component q100 os)`
 - ...or a more specific filter defined in CustomData:
```
[os]
filter=
|Component/BulletproofGlass q100
|Component/Canvas q100
|Component/Computer q100
|Component/Construction q1000
|Component/Detector q100
|Component/Display q100
|Component/Explosives q100
|Component/Girder q100
|Component/GravityGenerator q100
|Component/InteriorPlate q1000
|Component/LargeTube q100
|Component/Medical q100
|Component/MetalGrid q100
|Component/Motor q100
|Component/PowerCell q100
|Component/RadioCommunication q100
|Component/Reactor q100
|Component/SmallTube q100
|Component/SolarCell q100
|Component/SteelPlate q1000
|Component/Superconductor q100
|Component/Thrust q100
```

## Assemblers

Assembler inventories will only be pulled from when not crafting. No more scattered materials! Assemblers can be assigned as master by including `master` in the name (not case sensitive). This means that all other assemblers will automatically be set to Cooperative Mode. A typical setup would be one master assembler and a bunch of slaves, and you would only interact with the master. Only assemblers stationed on the same grid as the OS Programmable Block will be managed in this way.

## Power

OS can do more than just manage your items, such as automatically toggling your reactors. This means that if you have at least one battery and reactor, the reactor will automatically turn off if your batteries are almost full (80% by default) and vice verca (40% by default). If you are using more power than you can currently supply with batteries, the reactors will turn on. Only reactors stationed on the same grid as the OS Programmable Block will be managed in this way.

## O2/H2

OS can, as with power, manage your O2/H2 generators based on stored gas. This means that if either your oxygen or hydrogen storage is running low, the generator will be turned on and vice verca (60% and 90% by default). Only O2/H2 generators stationed on the same grid as the OS Programmable Block will be managed in this way.

## Performance

OS allows you to specify roughly how many instructions it is allowed to make per cycle. This value can be very low and it directly relates to server load. The default value is very low, so you probably don't need to touch this in most cases. For very complex grids you might want to increase the number if you think the script is operating too slow.

Only one instance of OS will transfer items per "supergrid". This means that if you dock a ship to a base, both running OS, only one script will transfer items.

## Debugging

By default, OS will print a form of log in the terminal. Select the Programmable Block and the log will be in the right section. If you have an invalid filter, or if something went wrong, this is the place you want to look. The error handler will print all errors here, including filters that OS failed to parse (with block name).

## Configuration

Most things the script does can be configured in the top part of the program. Variable names should be pretty self-explanatory but most noteworthy is `defaultFilters`. This is where you can change how "hands-off" the script should be. For example, O2/H2 Generators have a default filter of `"ice p100"`. If you replace this part with `""`, generators will no longer pull ice by default. They will still be managed though, which means they will no longer use the conveyor system and auto refill for bottles will be enabled. To disable this behaviour, change `manageGasGenerators` to `false`.

## Cheat Sheet
Items can be defined either by group (`Ingot`), name (`Cobalt`) or both (`Ingot/Cobalt`). Not case sensitive. Both group and name will be matched from the beginning, meaning `ing` -> `Ingot`, `cob` -> `Cobalt` (both ore and ingot) and `ing/c` -> `Ingot/Cobalt`.

Spaces will be ignored when matching, so `Welder4` -> `Welder 4`.
```
[Group]       [Name]
Ammo          Autocannon Clip
Ammo          Automatic Rifle Magazine
Ammo          Elite Pistol Magazine
Ammo          Full Auto Pistol Magazine
Ammo          Large Calibre Ammo
Ammo          Large Railgun Ammo
Ammo          Medium Calibre Ammo
Ammo          Missile 200mm
Ammo          NATO_25x184mm
Ammo          NATO_5p56x45mm
Ammo          Precise Automatic Rifle Magazine
Ammo          Rapid Fire Automatic Rifle Magazine
Ammo          Semi Auto Pistol Magazine
Ammo          Small Railgun Ammo
Ammo          Ultimate Automatic Rifle Magazine
Bottle        Hydrogen Bottle
Bottle        Oxygen Bottle
Component     Bulletproof Glass
Component     Canvas
Component     Computer
Component     Construction
Component     Detector
Component     Display
Component     Engineer Plushie
Component     Explosives
Component     Field_Modulators
Component     Girder
Component     Gravity Generator
Component     Interior Plate
Component     Large Tube
Component     Medical
Component     Metal Grid
Component     Motor
Component     Power Cell
Component     Radio Communication
Component     Reactor
Component     Small Tube
Component     Solar Cell
Component     Steel Plate
Component     Superconductor
Component     Thrust
Component     Zone Chip
Ingot         Cobalt
Ingot         Gold
Ingot         Gravel
Ingot         Iron
Ingot         Magnesium
Ingot         Nickel
Ingot         Platinum
Ingot         Scrap
Ingot         Silicon
Ingot         Silver
Ingot         Uranium
Ore           Cobalt
Ore           Gold
Ore           Ice
Ore           Iron
Ore           Magnesium
Ore           Nickel
Ore           Organic
Ore           Platinum
Ore           Scrap
Ore           Silicon
Ore           Silver
Ore           Stone
Ore           Uranium
Tool          Advanced Hand Held Launcher
Tool          Angle Grinder 2
Tool          Angle Grinder 3
Tool          Angle Grinder 4
Tool          Angle Grinder
Tool          Automatic Rifle
Tool          Basic Hand Held Launcher
Tool          Cube Placer
Tool          Elite Pistol
Tool          Full Auto Pistol
Tool          Good AIReward Punishment Tool
Tool          Hand Drill 2
Tool          Hand Drill 3
Tool          Hand Drill 4
Tool          Hand Drill
Tool          Precise Automatic Rifle
Tool          Rapid Fire Automatic Rifle
Tool          Semi Auto Pistol
Tool          Ultimate Automatic Rifle
Tool          Welder 2
Tool          Welder 3
Tool          Welder 4
Tool          Welder
```

<br>

# AutoInv

**DISCONTINUED**

> Powerful and efficient inventory manager with multiple levels of sorting. I created this in memory of TIM the almighty. But I want to surpass it in terms of versatility. AutoInv has its workload split up in several resumable states, so it should handle very large grids without a problem. Get it on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1675244660).
