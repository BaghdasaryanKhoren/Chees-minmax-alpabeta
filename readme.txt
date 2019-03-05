What's new (V2.05 -> v3.00)
    - Added difficulty levels
      Beginner:     Use a weak evaluation board (all pieces have the same value)
                    2-Ply search
                    No opening book
      Easy:         Use basic evaluation board
                    2-Ply search
                    No opening book
      Intermediate: Use basic evaluation board
                    4-Ply search
                    Unrated opening book
      Advance:      Use basic evaluation board
                    4-Ply search
                    Master opening book
      More Advance: Use basic evaluation board
                    6-Ply search
                    Master opening book
      Manual:       Define your own settings
    - Simplified the user interface
    - Added interface to the FICS (Free Internet Chess Server). You can now observe
      the following games in real time:
            Lightning / Blitz / Untimed and Standard time
    - Added tooltips in many dialog boxes and in the main interface
    - Added 'Load a Puzzle' command, which gives you access to more than 100 mates in N move games.
    - Added a warning for saving a board before leaving a game.
    - Moved the chessboard closer to the center
    - Rewrote the PGN parser to handle bigger PGN files and to be more compliant
      with PGN specifications.
    - The new parser comes with an improved advanced book and a new intermediate one. The 
      new books have been created from a 2.77 millions TWIC games. Thank you to chess.com for
      the SCID file. The advanced book includes games from players with ELO rating of 2500 or more.
    - Simplified status bar
    - Added a progress bar when finding a best move or waiting for a move from FICS server.
    - Did a major code clean-up
    - The game is now saving its last position and size.
    - To come: Let users play game via FICS server.

What's new (v2.04 -> v2.05)
    - Finally found this reentrance bug. If user press Ctrl-Z (Undo) or Ctrl-Y (Redo)
      fast enough, the undo command are being sent while the previous undo was stilled
      processed with two majors problems: 1) Crash, 2) Not refreshing every cell causing
      the shown board being different from the real one.
    - Added the Refresh (F5) option. It resynchronize the displayed board with the internal
      one. If you need this function, it's because there still a bug somewhere. If it happens,
      please let me know how you got it!

What's new (v2.03 -> v2.04)
    - Add a new File > Create Debugging Snapshot option to create a snapshot
      of the current game. This file can be used to reload the board as it was
      when the snapshot has been taken to diagnose error (example: when a valid position is not accepted).
    - Loading a PGN file was locking the file up to the next GC.
    - Program was crashing when trying to load an board with no moves.
    - Program was crashing when loading some valid FEN specification.
    - When saving a FEN representation of a board, the En passant position
      was the position of the pawn instead of the position behind the pawn.
    - Add support for FEN string in addition of a PGN one in File > Create Game
    - The PGNUtil.GetFENFromBoard now set correctly the Halfmove Clock and Fullmove count in the FEN string.
    - The ELO of white/black player was inversed in the description of the loading pane.
    - Improved code for "en passant" handling.
    - Don't allow player to clicked on the opponent piece.

What's new (v2.02 -> v2.03)
    - Add a button to load a PGN games without the moves (so, having the board setting only)

What's new (v2.01 -> V2.02)
    - Correct endless loop when loading or parsing pgn files

What's new (v2.00 -> V2.01)

    - The label for row position were inverse (1-8 instead of 8-1)
    - Player against player and Design Mode menu -> Selecting the menu get unsynchronize with the real state
      (or why you must never set the IsCheckable property to true when you have an accelerator key)
    - Typo in dialog title 'Test Board Avaluators' corrected to 'Test Board Evaluator'
    - Add the GPL license

What's new (v1.00 -> V2.00)

    - Move the application to WPF
    - New user interface
    - Add a list of piece sets to choose from
    - Thank you to Ilya Margolin for the XAML piece sets

What's new (0.943 -> v1.00)

  Game:
    - Permits pawn promotion to non-queen pieces
    
  Move Display:
    - Moves can now be seen using PGN format or Start/End position format
    - Start/End position format use 'x' when a piece is eated
    - Book move shown between parentheses are now preserved after being saved

  Search engine:
    - New searching mode: iterative depth-first search with a fix number of play
    - Correct problem with iterative search so it now perform better than before
    - Correct problem with point evaluation of draw in alpha-beta and min-max
    - Declare a draw when the minimum pieces requirement for checkmate is not met
    - Improve point evaluation by using the number of attacked pieces and the number of attacked positions
    
  Interface:
    - Add timer for white/black player
    - Use vector graphic to draw the pieces of the board to improve quality when board is resized.
    - Add a toolbar
    - Improves the status bar
    - Permits to undo the first move when in player vs player mode
    - Add option to select the color of the pieces and board
    - Improves messages related to the end of the game
 
  Loading / Saving
    - Board can now also be saved in PGN format
    - Saved format has changed a lot... so, it's not compatible with the old format
    - Correct bug which was discarding the move list when loading a PGN file with FEN included.
