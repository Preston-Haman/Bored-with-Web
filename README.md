# Bored-with-Web
This is a website where people can get together and play board games against each other. It was created as a class assignment during my time at a local college. The website uses a database to track user accounts and their associated gameplay statistics.

**Important: Playing games on this website *requires* a minimum of two players**.

## Currently Implemented Games
- Connect Four
- Checkers

## Preview
As it is difficult to browse a website in code form (via this repository), some demo videos of some of this website's features are below. These are excerpts from a presentation to my peers, recorded on a live version of the website that could formerly be found at `https://bored-with-web.azurewebsites.net/`, but has since been taken down.
___
Game Lobby

https://github.com/Preston-Haman/Bored-with-Web/assets/92552522/cd7bf259-7778-459b-84ed-7921de5cd2f4
___
Connect Four

https://github.com/Preston-Haman/Bored-with-Web/assets/92552522/c832c153-342f-495c-beb4-3364eb4ef093
___
Checkers

https://github.com/Preston-Haman/Bored-with-Web/assets/92552522/f85b25ef-2d87-4ad6-8b1c-f3ec3f74ae14
___
Joke API

https://github.com/Preston-Haman/Bored-with-Web/assets/92552522/0bb4a71a-dc52-4002-8c01-c17af1df6df8
___
# Initial Concept (Subject to Change)
A website where games can be played with other people

- You can play a game with random people chosen from a lobby.
- You can play a game through an invite link.
- You can create an invite link to join a game and invite someone else into it.
- You can spectate a game -- either through invite, or from the lobby of ongoing games.
- While playing or spectating a game, you can chat with others doing the same (spectators chat with spectators, and players chat with players).
- If you login, statistics about your gameplay will be logged and visible to you on a statistics page.
- If you don't login, your statistics will be tracked by your session; if you login before it expires, then it will be added to your account.
- The statistics page will offer a reset button (mainly for full CRUD functionality on at least one entity).
- If you were a player of a simple game, you can spectate a replay of that game through your statistics page.


### Simple Games
- Connect Four
- Checkers
- Chess

### Other Games
- HF-S Project

### Pages
____
#### Home Page
- Offer login/registration to the user
- Mention games that are available
- Blurb about site

#### Account Page(s)
- Offer account management
- Offer to view previously played games (spectate a controllable replay)
- Display statistics about each game (win/loss, etc.)
- Offer a reset stats button for each game's statistics

#### Game Selection Page
- Display available games and allow the user to pick one to play

#### Game Description Page (one per game?)
- Discuss the game and its rules
- Maybe merge with selection page

#### Game Lobby Page (one per game?)
- Shows ongoing games to spectate (minus invite-only games)
- Has a waiting room to join a random game
- Shows other players in the lobby

#### Gameplay Page (one per game)
- Playable version of the game

____
All rights reserved.
