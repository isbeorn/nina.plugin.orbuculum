# Changelog

## Version 2.0.0.0
- Upgrade to .NET7

## Version 1.0.5.0
- Add "Wait for Hour Angle" instruction

## Version 1.0.4.3
- Prevent unhandled exception when using "Loop while hour angle" without a target available
- Show double dashes instead of "NaN" when not target is available

## Version 1.0.4.2
- Prevent deadlock when using orbitals sequences as next target
- Set next target name properly when it is changed

## Version 1.0.4.0
- Show estimated time when altitude is reached

## Version 1.0.3.4
- Properly consider disable status for orbuculum condition loops

## Version 1.0.3.3
- Added more descriptions about available instructions
- Allow for a ratio of 0 for `🧙 Auto Balancing Exposure` to disable a row temporarily

## Version 1.0.3.2
- `🧙 Auto Balancing Exposure` was not working correctly with GroundStation Failure Trigger. Added a workaround to make Groundstation not fail with the instruction.

## Version 1.0.3.1
- Fixed an issue where the `🧙 Auto Balancing Exposure` was not populating the target name
- Changed the stepper to not show decimal places for the ratio

## Version 1.0.3.0
- Added `🧙 Auto Balancing Exposure` - An instruction to automatically balance exposure count by progress and weight with a defined list of exposure info
- Added `🕑 Loop While Hour Angle` and `🕑 Loop While Next Target Hour Angle` loop conditions to loop using hour angle
- Fixed some issues where the existing conditions did not copy all relevant settings

## Version 1.0.2.0
- Move code to separate repository

## Version 1.0.1.0

- Added offset for `🔮 Loop While Next Target Is Below Horizon` instruction

## Version 1.0.0.0

- Initial release