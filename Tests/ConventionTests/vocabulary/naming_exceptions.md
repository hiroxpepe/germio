# Naming Exceptions

The standard here is set once, in full, in `coding_standard.md`, and the
same for every project (webio, animo, briko, germio, and the like) — a
public, internal, or protected member is PascalCase, because all three
face a reader who is not the author: internal faces every other file in
the same assembly, and protected faces every subclass, which may live in
a project this repository never sees. Neither is truly private.

This file exists for the rare case where a project has already, on
purpose, given a specific member a different shape, and changing it now
would cost more than it is worth. An entry here is not a second standard;
it is a named, reviewed exception to the one standard, kept in exactly
one place so it stays visible instead of spreading. Most projects keep
this file empty.

Each line names one exact member, written as `TypeName.member_name`, so
an unrelated member elsewhere with the same short name is never
accidentally covered.

+ Block.abilities_OnAwake
+ Block.abilities_OnStart
+ GameSystem.abilities_OnAwake
+ GameSystem.abilities_OnStart
+ Human.abilities_OnAwake
+ Human.abilities_OnStart
+ NoticeSystem.abilities_OnAwake
+ NoticeSystem.abilities_OnStart
+ Human._JUMP_POWER
+ Human._ROTATIONAL_SPEED
+ Human._FORWARD_SPEED_LIMIT
+ Human._RUN_SPEED_LIMIT
+ Human._BACKWARD_SPEED_LIMIT
+ Common._CAN_HOLD
+ Common._HOLD_ADJUST_Y
+ Common._HOLD_ADJUST_X_OR_Z
+ Common._HOLD_ADJUST_DEGREE
+ InputMapper.A_Button
+ InputMapper.B_Button
+ InputMapper.X_Button
+ InputMapper.Y_Button
+ InputMapper.Up_Button
+ InputMapper.Down_Button
+ InputMapper.Left_Button
+ InputMapper.Right_Button
+ InputMapper.Left1_Button
+ InputMapper.Right1_Button
+ InputMapper.Left2_Button
+ InputMapper.Right2_Button
+ InputMapper.RightStick_Up_Button
+ InputMapper.RightStick_Down_Button
+ InputMapper.RightStick_Left_Button
+ InputMapper.RightStick_Right_Button
+ InputMapper.RightStick_Button
+ InputMapper.Start_Button
+ InputMapper.Select_Button
+ _
+ InputMapper.Left1_Button
+ InputMapper.Right1_Button
+ InputMapper.Left2_Button
+ InputMapper.Right2_Button
+ Human_Extensions
