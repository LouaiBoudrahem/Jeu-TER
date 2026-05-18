# Room Quiz Event System - Setup Guide

## Overview
This system creates a complete room-entry quiz experience with:
- Lights turning on when the player enters
- Entry and quiz start audio
- 5 multiple-choice questions with 8-second timer per question
- Clock ticking SFX during answer period
- Score evaluation (≥3 correct = pass)
- Supervisor spawning on fail (chases for 15 seconds)
- Door unlocking on completion

## Components Created

### 1. **RoomQuizEventManager.cs**
Main manager that orchestrates the entire quiz experience. Attach this to a GameObject with a trigger collider in your room.

### 2. **SupervisorChaseController.cs**
Handles supervisor AI - chases the player using NavMesh for a limited duration.

### 3. **Updated QuestionData.cs**
Added `AudioClip audioClip` field to support per-question audio.

---

## Scene Setup Instructions

### Step 1: Create Quiz Room Setup
1. Create or use an existing room GameObject with a trigger collider
2. Add the **RoomQuizEventManager** script to this GameObject
3. Ensure the collider is set to `Is Trigger = true`

### Step 2: Configure RoomQuizEventManager Inspector

#### Room Trigger
- **Room Trigger**: Auto-filled from GetComponent, or manually assign the trigger collider
- **Room Light**: Drag the Light component that should turn on

#### Quiz Configuration
- **Quiz Questions** (array of 5): 
  - Create or assign 5 `QuestionData` ScriptableObjects
  - Each must be Multiple Choice type
  - Each needs 4 options and a `correctOptionIndex`
  - Optional: Add an `audioClip` for question narration

- **Question Duration**: Set to 8.0 (8 seconds per question)

#### Audio
Assign your audio clips:
- **Lights On Clip**: SFX for lights turning on
- **Quiz Start Clip**: Audio that plays before first question
- **Ticking Sound Clip**: Clock ticking SFX (loops during countdown)
- **Satisfied Clip**: Audio when player passes (≥3 correct)
- **Unsatisfied Clip**: Audio when player fails (<3 correct)
- **Audio Source**: Auto-created or assign existing AudioSource

#### UI - Quiz Canvas
Create a Canvas (Screen Space) with these UI elements:
- **Question Text**: TextMeshProUGUI showing the current question
- **Choice Buttons**: 4 Buttons (each should have a TextMeshProUGUI child for the option text)
- **Timer Display**: Image component for visual timer (fill amount shows remaining time)

You can copy the layout from the existing Quiz UI if you have one.

#### Door
- **Door Controller**: Drag the DoorController component of the door you want to unlock
- Ensure the DoorController has `OpenDoor()` method (it does by default)

#### Supervisor
- **Supervisor Prefab**: Your supervisor character prefab
  - Should have a model/visual representation
  - Will automatically get a NavMeshAgent added
  - NavMesh must be baked in your scene!
- **Supervisor Chase Distance**: 5 units (offset from player spawn position)
- **Supervisor Chase Duration**: 15 seconds (how long supervisor chases before disappearing)

### Step 3: Prepare Question Data

For each of your 5 questions:
1. Right-click in Project → Create → Question Data
2. Set the following:
   - **Question Type**: Multiple Choice
   - **Question**: The question text
   - **Options**: Array of 4 answer choices
   - **Correct Option Index**: The index (0-3) of the correct answer
   - **Audio Clip**: (Optional) Narration audio for this question

### Step 4: Create Quiz UI Canvas

If you don't have one:
1. Create a new Canvas (Screen Space Overlay)
2. Add a Panel for the background
3. Create text and button hierarchy:
   ```
   Canvas
   ├── Panel (background)
   ├── QuestionText (TextMeshProUGUI)
   ├── ChoiceButtonsContainer (empty GameObject)
   │   ├── ChoiceButton1 (Button)
   │   │   └── Text (TextMeshProUGUI)
   │   ├── ChoiceButton2 (Button)
   │   │   └── Text (TextMeshProUGUI)
   │   ├── ChoiceButton3 (Button)
   │   │   └── Text (TextMeshProUGUI)
   │   └── ChoiceButton4 (Button)
   │       └── Text (TextMeshProUGUI)
   └── TimerImage (Image - white square, use fill method: "Filled" with fillMethod "Horizontal")
   ```
4. Start with Canvas disabled (it will be enabled when quiz starts)
5. Assign all these UI elements to the RoomQuizEventManager

### Step 5: NavMesh Setup

**CRITICAL**: You must have a NavMesh baked in your scene for the supervisor to chase:
1. Go to Window → AI → Navigation
2. Select your room/level geometry
3. Mark as "Walkable" and adjust agent settings if needed
4. Click "Bake"

---

## How It Works

### Trigger Flow
1. Player enters room trigger → `OnTriggerEnter` fires
2. Lights turn on, lights-on SFX plays
3. Quiz UI appears, quiz-start SFX plays
4. First question audio plays (if assigned)
5. Question text and 4 buttons appear
6. 8-second timer starts with ticking SFX
7. Player clicks an answer button
8. Move to next question (repeat steps 4-7)
9. After all 5 questions, evaluate score

### Score Evaluation
- If `correctAnswerCount >= 3`: Play satisfied audio, unlock door
- If `correctAnswerCount < 3`: Play unsatisfied audio, spawn supervisor, supervisor chases for 15 seconds, then unlock door

### After Quiz
- Door is unlocked/opened
- Quiz UI hidden
- Quiz event marked as complete (won't trigger again even if player re-enters)

---

## Customization Options

### Adjust Timer Duration
Change `questionDuration` to something other than 8 seconds

### Change Supervisor Chase Time
Modify `supervisorChaseDuration` (default 15 seconds)

### Change Pass Threshold
In `EvaluateQuizResults()`, change `correctAnswerCount >= 3` to a different threshold

### Add More Questions
Expand the `quizQuestions` array to more than 5 questions

### Disable Supervisor Spawning
Set `supervisorPrefab` to null

---

## Troubleshooting

### Quiz doesn't start
- Check that the room trigger collider is set to `Is Trigger = true`
- Verify Player object has collider
- Check console for warnings

### Questions don't appear
- Ensure all 5 QuestionData slots are filled
- Verify QuestionType is "Multiple Choice"
- Check that UI elements are properly assigned

### Supervisor doesn't chase
- **Most common**: NavMesh not baked! Go to Window → AI → Navigation and bake
- Verify `supervisorPrefab` is assigned
- Check supervisor has a Renderer (visual model)

### Timer doesn't show
- Verify TimerImage component is assigned
- Check that the Image has `Ray cast Target = true` if you want it to block clicks
- Set Image's fill method to "Filled" and fill method to "Horizontal"

### Door doesn't open
- Verify DoorController is assigned
- Ensure DoorController's OpenDoor() method isn't overridden
- Check that door doesn't have any locks preventing it

---

## Audio Requirements

Make sure you have all these audio clips ready:
- **Lights On**: Brief SFX (1-2 seconds)
- **Quiz Start**: Intro music/sound (2-4 seconds)
- **Ticking**: Clock/timer SFX (looped, ~0.5 second sample)
- **Satisfied**: Happy/positive SFX (1-3 seconds)
- **Unsatisfied**: Sad/negative SFX (1-3 seconds)
- **Per-question audio** (optional): Narration for each question

---

## Example Setup Complete!
Once you've set this all up, the quiz will:
1. Trigger when player enters
2. Show all UI and audio
3. Play 5 questions in sequence
4. Evaluate results
5. Unlock door and spawn supervisor if failed
6. Never trigger again for that player

Enjoy your quiz system! 🎓
