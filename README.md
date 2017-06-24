# DeckTracker v1.0.43
Automatic in-game **Universal Deck Tracker** for collectible card games such as [**The Elder Scrolls: Legends**](https://legends.bethesda.net) and [**Eternal**](https://www.eternalcardgame.com/). Unlike all other trackers for these games, this one *does not* modify game files so it is compliant with Terms of Service agreements. Using trackers that modify game files might get your account banned. As an additional benefit it doesn't break when the game update is released.

This tracker also tries to automatically classify the archetype of the decks you were playing against so that winrates are displayed separately per deck type. This algorithm is work in progress and the configuration file that specifies all deck types is located [here](decktypes.txt). Feel free to [suggest](https://github.com/extesy/DeckTracker/issues) improvements to it.

# Features
* Detects all your decks, no manual entry required.
* Automatic in-game tracking of the remaining cards and the opponent's played cards.
* No separate overlay capture window needed - very convenient for twitch streamers.
* Tracks winrates for your decks against each type of the opponent decks.
* Instanteneously import your deck from [legends-decks.com](http://www.legends-decks.com) or [eternalwarcry.com](https://eternalwarcry.com) using your premium cards first.
* Easy export of your entire collection in a standard format.

![Eternal](https://cloud.githubusercontent.com/assets/65872/26518058/aba71222-425c-11e7-8392-ed9981a23c8b.jpg)

![The Elder Scrolls: Legends](https://user-images.githubusercontent.com/65872/27020491-b9daea98-4ef6-11e7-8ce4-7c59df1853a7.jpg)

## Keyboard shortcuts
* F1 - show/hide entire Deck Tracker UI
* F2 - show/hide player's deck
* F3 - show/hide opponent's deck
* F4 - show/hide deck header with deck archetypes and win rates
* F5 - (Eternal only) show/hide current player's score (only for ranks below Masters)
* F11 - show/hide random deck from the player's collection for test purposes

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
* How to resize deck lists?
> Resizer area is a strip located along the right side and also along the bottom side of the deck list. Drag and drop in that area to resize.
* How to change detected deck archetypes or add new ones?
> Create or update `C:\Users\{profile}\AppData\Roaming\UniversalDeckTracker\decktypes.txt` file. If you like your changes, please contribute back to the community by sending a pull request.
* Does this deck tracker share any data?
> Yes, it automatically uploads game replays for aggregation and metagame analysis. Nothing else is shared.

## Known issues
* Some antivirus software is known to show false positives on this deck tracker. Please add it (along with the folder `C:\Users\{profile}\AppData\Local\UniversalDeckTracker`) to the exclusion list.
* If deck tracker is started after the game client then it sometimes might not be able to fetch deck lists from the collection. Solution: start the tracker before the game.
* After reconnecting to the game in progress the full deck list might not be visible or the counts might not reflect the cards already played. This is caused by the game reconnect protocol that doesn't send the deck data but only the current board state.

## Reporting problems
Please [open the issue](https://github.com/extesy/DeckTracker/issues) and describe the problem and the steps to reproduce it. If the problem is visual then please also include screenshots.

In some cases it is also necessary to attach debug log files. To enable debug logging launch the tracker with `--debug` command line parameter. You can either update the desktop shortcut to include this parameter or use command line. Debug logs will be available at `C:\Users\{profile}\AppData\Roaming\UniversalDeckTracker` location and have `*.log` extension.
