# DeckTracker v1.0.11
Automatic in-game **Universal Deck Tracker** for collectible card games such as [**The Elder Scrolls: Legends**](https://legends.bethesda.net) and [**Eternal**](https://www.eternalcardgame.com/). Unlike all other trackers for these games, this one *does not* modify game files so it is compliant with Terms of Service agreements. Using trackers that modify game files might get your account banned. As an additional benefit it doesn't break when the game update is released.

This tracker also tries to automatically classify the archetype of the decks you were playing against so that winrates are displayed separately per deck type. This algorithm is work in progress and the configuration file that specifies all deck types is located [here](decktypes.txt). Feel free to [suggest](https://github.com/extesy/DeckTracker/issues) improvements to it.

## Keyboard shortcuts
* F1 - show/hide entire Deck Tracker UI
* F2 - show/hide player's deck
* F3 - show/hide opponent's deck
* F4 - show/hide rank display (only for Eternal)
* F5 - show/hide random deck from the player's collection for test purposes

## Installation
For the initial install please download the [UniversalDeckTracker.exe](https://github.com/extesy/DeckTracker/releases/latest) distributive. Running the installer will create a desktop shortcut and put the game files into `C:\Users\{profile}\AppData\Local\UniversalDeckTracker` location.

## Update
There are two options:
1. When the Deck Tracker is starting, it will attempt to auto-update. If a new build is available, it will be downloaded and applied in background, so after restarting the tracker it will use a new version. This normally happens in a first few seconds to a minute after launching the tracker.
2. Downloading the [latest installer](https://github.com/extesy/DeckTracker/releases/latest) and running it will achieve the same thing.

## Uninstall
Use the standard Windows `Add or Remove Programs` window and find `Universal Deck Tracker` closer to the end of the list.

## FAQ: Frequently Asked Questions
* How to disable the in-game UI but still track winrates?
> Press F1 (and see the [keyboard shortcuts](#keyboard-shortcuts) section)
* How to move the deck lists on the screen to a different position?
> Just drag and drop the deck list.

## Known issues
* If deck tracker is started after the game client then it sometimes might not be able to fetch deck lists from the collection. Solution: start the tracker before the game.
* After reconnecting to the game in progress the full deck list might not be visible or the counts might not reflect the cards already played. This is caused by the game reconnect protocol that doesn't send the deck data but only the current board state.

## Reporting problems
Please [open the issue](https://github.com/extesy/DeckTracker/issues) and describe the problem and the steps to reproduce it. If the problem is visual then please also include screenshots.

In some cases it is also necessary to attach debug log files. To enable debug logging launch the tracker with `--debug` command line parameter. You can either update the desktop shortcut to include this parameter or use command line. Debug logs will be available at `C:\Users\{profile}\AppData\Roaming\UniversalDeckTracker` location and have `*.log` extension.
