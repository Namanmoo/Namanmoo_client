# Title Screen Design

## Goal

Start the game at a title screen based on `Title.png`. Clicking the pictured
game-start button opens Stage1. The pictured settings button remains
nonfunctional.

## Layout

- Import `Title.png` as `Assets/UI/Title.png`.
- Display it on a screen-space overlay Canvas at a 1920x1080 reference
  resolution with aspect preservation.
- Place transparent click targets over the game-start and settings rectangles
  drawn into the image.
- The game-start target loads `Assets/Scenes/Stage1.unity`.
- The settings target has no click listener.
- Add an EventSystem using the Input System UI module.

## Startup

Create `Assets/Scenes/Title.unity` and configure enabled build scenes in this
order:

1. `Assets/Scenes/Title.unity`
2. `Assets/Scenes/Stage1.unity`

## Testing

- The title texture imports as a single sprite without mipmaps.
- The title scene contains a full-screen image, two click targets, an
  EventSystem, and exactly one start action.
- The loader resolves Stage1 by path.
- Build settings list Title first and Stage1 second.
