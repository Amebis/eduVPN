# ConnectingProfileSelectMode - Client Profile Management Mode

According to the [#28](https://github.com/Amebis/eduVPN/issues/28) four distinct modes of client operation were introduced. They are labelled as 0 (default), 1, 2, and 3. The mode affect behaviour of locally authenticated instance sources only (Institute Access).

The mode of operation can be set in `eduVPN.Client.exe.config` XML file. When changing mode, the client should be closed. Furthermore, it is recommended to delete client user settings in `%LOCALAPPDATA%\SURF` when the mode is changed.


## Mode 0

![Mode 0](ConnectingProfileSelectMode/mode0.png "Mode 0 Flowchart")

Only the profiles user connects to are added to the list of recent profile selection. The list of recent profile selection is maintained automatically. The user can remove profiles from the list using the context menu.

This mode is the compacter form of [Mode 2](#mode-2).

Cons:
- Users will not notice newly available profiles.
- The client needs to load `/profile_list` from all recently connected instance on startup to see whether any profile is no longer available.


## Mode 1

![Mode 1](ConnectingProfileSelectMode/mode1.png "Mode 1 Flowchart")

The user manages the list of recent instance selection manually. The user can add new instances using the standard procedure, and remove them using the context menu.

To connect, the user selects the instance from the list. The profile selection page is displayed for specific instance next, where the user selects the profile to connect to.

Pros:
- `/profile_list` is loaded on demand. Client startup is faster.

Cons:
- The user needs an extra step to connect. However, profile selection could be ommited when instance has only one profile.


## Mode 2

![Mode 2](ConnectingProfileSelectMode/mode2.png "Mode 2 Flowchart")

All profiles of instances user connects to are added to the list of recent profile selection. This allows the list to grow when new profiles are added to the connecting instances later.

This mode is the extended form of [Mode 0](#mode-0).

Cons:
- The client needs to load `/profile_list` from all recently connected instance on startup to prepare the list of profiles.
- The list might grow quite long, should the user connect to a lot of instances and/or some instances having a lot of profiles to connect to.


## Mode 3

![Mode 3](ConnectingProfileSelectMode/mode3.png "Mode 3 Flowchart")

The user manages the list of recent instance selection manually. The user can add new instances using the standard procedure, and remove them using the context menu.

To connect, the user selects the instance from the drop-down list. Next, the list of available profiles is displayed below, where the user selects the profile to connect to.

Pros:
- The experience is the same as with other instance source types (Secure Internet)
- `/profile_list` is loaded on demand. Client startup is faster.

Cons:
- The user needs three clicks to change the instance and select the profile to connect to.
- When Secure Internet and Institute Access are displayed on the same page simultaneously, the UI gets crammed.
