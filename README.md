# Slay the Beat
<a href="https://ibb.co/gFLLzLfy"><img src="https://i.ibb.co/h1JJDJ4s/store-capsule-header.png" alt="store-capsule-header" border="0" /></a>
*Figure 1: SLay the Beat Capsule Header*

**Unit Name:** Final Major Project

**Student Name:** Ihor Syvanenko

**Student ID:** 2308083

**Total Word Count:** ≈ 4500

**Documentation Link:** \[[GitHub Pages Doxygn](https://isyvanenko.github.io/Slay-the-beat--Unity-5-/)]

**Repository Link:** \[[GitHub Slay The Beat](https://github.com/isyvanenko/Slay-the-beat--Unity-5-)]

**Build Link:** \[[ITCH.IO GAME PAGE](https://perisinch.itch.io/slay)]

**Video Demonstration Link:** \[[EARLY GAMEPLAY](https://youtu.be/nWXdQ-Oh4nE)]

---

## Abstract 

Create an *old-style arcade game* with a *modern twist*. Use a fan favourite mechanic, like frantic dodging, and add stunningly modern graphics and sound design to it. Make a game that can be played as an exciting party game at a local offline competitive mode where friends furiously clash. Enhance this by making the rhythm-based game focused heavily on the idea of hitting the exact ideal ms for perfect timing. Create a truly unique immersive experience with every song, seamlessly altering the glowing arena. Make the player feel completely in control of the main character with fluid procedural animations constantly reacting to user inputs. Ultimately, deliver a flawlessly polished experience, easily worth uploading to Steam and making it a brand.
<a href="https://ibb.co/QLGKc5C"><img src="https://i.ibb.co/0T3GXwF/Screenshot-2026-05-14-at-12-32-05.png" alt="Screenshot-2026-05-14-at-12-32-05" border="0" /></a>
*Figure 2: Prototyping stage demo assets*


---

## Research 

### What sources or references have you identified as relevant to this task?

For my project, I needed **firsthand experience** with classic, old-style arcade rhythm machines, such as Dance Dance Revolution and Stepmania. To truly understand their lasting appeal, I took a dedicated trip around several vibrant local arcades to deeply analyze the core gameplay loop and soak in the energetic, neon-lit atmosphere. By physically playing these iconic games in a real-world setting, I quickly gained invaluable, hands-on experience of the demanding mechanics. This immersive research finally allowed me to precisely identify exactly what a competitive player is looking for in rhythm-style games, from the tactile pad feedback to the addictive, high-energy pacing. 
![Arcade cabinet](https://static.wixstatic.com/media/7b8070_42dbc2ee303a46b7a6a1e680d05a795c~mv2.jpg/v1/fill/w_700,h_774,al_c,q_85,usm_0.66_1.00_0.01,enc_avif,quality_auto/7b8070_42dbc2ee303a46b7a6a1e680d05a795c~mv2.jpg)
*Figure 3: DDR arcade cabinet.*


Another incredibly valuable source of inspiration and reference for defining the target graphics and overall user experience came from watching archival video recordings of classic Dance Dance Revolution interfaces on YouTube. While playing physical cabinets provided a tactile understanding, studying these gameplay videos allowed me to analytically pause, rewind, and dissect older UI designs without the pressure of live gameplay.

These retrospectives offered a profound new understanding of effective UX in high-speed, competitive environments. I closely observed how legacy titles utilized visual hierarchies, flashing combo indicators, and scrolling inputs to communicate crucial information to players in fractions of a second. However, this deep dive also highlighted the limitations of the era, such as cluttered menu screens and visually overwhelming feedback loops.

<iframe width="560" height="315" src="https://www.youtube.com/embed/sajMR0P8LAs?list=PL5qePuWio0Gn_H47B1sRDWVTj3uA416JS" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

*Figure 4: DDR Eurostage song list*

To ensure players receive the most satisfying and intuitive in-game experience, I needed to deeply understand the underlying logic of arrow spawning. It is not simply about placing arrows on a musical grid; it requires studying how the human brain actually processes beat and rhythm. During this research, I discovered a fascinating neurological concept called **Action Simulation for Auditory Prediction (ASAP)**. ASAP suggests that our motor systems actively simulate movements to predict upcoming beats, meaning players anticipate musical cues milliseconds before they actually hear them. To effectively translate this psychological anticipation into smooth game mechanics, I required precise latency targets. I consulted Wikipedia for foundational cognitive science and heavily relied on the research paper "Switch-JustDance: Benchmarking Whole Body Motion Tracking Controllers." This academic benchmarking was crucial. It helped me calculate the exact millisecond input delay threshold I must aim for to perfectly align hardware responsiveness with the player's internal metronome.


<a href="https://ibb.co/5gTXPmsM"><img src="https://i.ibb.co/pB26Sc40/Screenshot-2026-05-14-at-15-41-16.png" alt="Screenshot-2026-05-14-at-15-41-16" border="0" /></a>
*Figure 5: Screenshot from GDC talk on Sync VS Feel*

I further explored modern hardware capabilities to maximize performance and responsiveness. By scouring Unity Forums and technical documentation, I gained a deep understanding of .dspTime. Implementing this high-precision clock ensures my rhythm engine stays perfectly synced with the audio thread, bypassing frame-rate fluctuations for a professional, lag-free, and truly immersive experience.

Also, an important part of my research was to understand where to get the music for the stages from. Paying a licence fee cost from 100£ to 10000£ yeary. As a small creator of an indie game I dont have such a finence so I had to use generative audio AI called Suno.AI which cost only £6.77 a month and I have full comercial rights to it.  

When I was searching for the tool for creating music for my game, I came across Google Gemeni's new Song creation system, which, on the first try, was a great free choice, but it only creates demo songs up to 40 seconds, where I needed a fully produced song. This might be a great option for musicians who can create a song with a bit of inspiration.

|  | Gemini | Suno.AI |
| :--- | :--- | :--- |
| **Price** | Free | Free + Paid Options |
| **Song Length** | 30s | Up to 10 min |
| **Custom lyrics** | No | Yes |
| **Commercial rights** | No | Yes |

#### Sources

##### **Music generation:**
I needed to find relevant sources for generating music for my game. I could have gone with free but quite limited Lyria 3 from Google, but it would be limited to music creation and commercial rights. 

https://gemini.google/overview/music-generation/ - Gemini Lyria 3

https://suno.com/terms-of-service - Suno.AI terms and conditions 

https://www.reddit.com/r/MusicNotes/comments/1pd3vgm/whats_the_best_ai_music_generator_reddit_vote/ - Redit foru, where users share their favorite music generation AI.

<iframe width="560" height="315" src="https://www.youtube.com/embed/dXMyXIDoimU?si=3adprLYwH3_gPe_x" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

*Figure 6: Youtube video Better than Suno?*


From the forum related to music AI, I discovered that Suno.AI's paid option is the best option because it allows me to use the music commercially for the lowest price on the market. Also, I still own the rights to the song even if I cancel the membership, as stated in the terms and conditions. 

##### **Visual insperation:**

needed to find relevant visual references and inspiration for the UI and presentation style of my rhythm game. I researched existing dance and rhythm games to analyse how they handle gameplay visuals, song selection menus, and overall player feedback.

https://www.youtube.com/watch?v=sajMR0P8LAs&list=PL5qePuWio0Gn_H47B1sRDWVTj3uA416JS - Dance Stage EuroMIX gameplay

https://www.youtube.com/watch?v=KcpLzLIUlr8&list=RDKcpLzLIUlr8&start_radio=1 - Dance Stage EuroMIX song selection

https://www.youtube.com/watch?v=pZHBC795baw&list=PL9Vs-i0zjucqhMK-_YeYGVsXHtIyfpHF4&index=15 - Just Dance 4 Song List

<iframe width="560" height="315" src="https://www.youtube.com/embed/XygcmQw5mIE?si=8Qilz9IKAvc5X8rz" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

*Figure 7: SLAY demo UI showcase inspired by DDR?*

From these references, I discovered that Dance Stage EuroMIX had a strong arcade-style presentation with animated menus, bold UI elements, and clear gameplay readability that would work well for my project. I also analysed the song selection interface to understand how songs are presented to the player in an engaging way. Additionally, Just Dance 4 provided inspiration for colourful visuals, energetic transitions, and overall stage presentation that could help make my game feel more lively and interactive.

##### **Logic for creatring a perfect rhytm based game:**
I needed to research the logic and psychology behind creating an engaging rhythm-based game. To better understand how players process rhythm, timing, and movement, I analysed scientific articles related to beat perception and rhythm game interaction systems.

https://pmc.ncbi.nlm.nih.gov/articles/PMC4026735/ - Scientific article about the Action Simulation for Auditory Prediction (ASAP) hypothesis

https://www.sciencedirect.com/science/article/abs/pii/S1364661320302746 - Article discussing ASAP and action simulation in rhythm perception

https://pmc.ncbi.nlm.nih.gov/articles/PMC5447290/ - Article about rhythm-based processing and perception

<iframe width="560" height="315" src="https://www.youtube.com/embed/Bl67bT_XSHY?si=JDiF8dLn5byDdUK4" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

*Figure 7: My youtube video explaining the ASAP in rhytm based games?*



From these studies, I discovered that rhythm perception is strongly connected to the brain’s motor planning system, which explains why players naturally react to musical beats with physical movement. The ASAP hypothesis explains how the brain predicts upcoming beats by internally simulating movement patterns, which is important for designing responsive gameplay and accurate note timing. The research also highlighted the importance of synchronisation, visual feedback, and predictive timing in rhythm games, helping me understand how to create gameplay that feels smooth, satisfying, and intuitive for players.

---

## Implementation 

### Main Game Logic
The first big challenge I needed to concentrate on was to create a system that could spawn arrows (notes) at the right timing and at the right place. The first thing that I needed to do was to create an actual note prefab that can be spawned from the manager later on. First, I made the 2D sprite asset in Adobe Photoshop and using Unity Animations, I was able to animate it and bring it to life. The best solution was to create a sprite sheet that can be later cut in the engine to separate images. That helps with optimisation and would make the actual file smaller than uploading different images. (Figure 8)

<a href="https://ibb.co/gZ75XCMG"><img src="https://i.ibb.co/F4WSftb9/spritesheet-3.png" alt="spritesheet-3" border="0" /></a>
*Figure 8: Arrow visual asset png spritesheet.*

For Arrow object logic I created the script that works something like this: Spawner creates note

1. **Init() sets timing**
```csharp
 public void Init(double songHitTime, double songSpawnTime, float noteSpeed, GameplayManager gameManager, PlayerScoreManager scoreMgr, float missY, LaneController lane)
    {
        targetTime = songHitTime;
        spawnTime = songSpawnTime;
        speed = noteSpeed;
        manager = gameManager;
        myScoreManager = scoreMgr;
        missTriggerY = missY;
        parentLane = lane;
        
        startY = rectTransform.anchoredPosition.y; 
    }
```
2. **Update() moves note based on song time**
```csharp
    void Update()
    {
        // Making sure we DO NOT MOVE WHEN PAUSE IS ACTIVE (!!!!)
        if (isPaused || manager == null || manager.IsPaused())
        {
            return;
        }
        
        // Only move if not paused
        double currentSongTime = manager.GetAdjustedSongTime();
        double timeAlive = currentSongTime - spawnTime;
        float newY = startY + (float)(timeAlive * speed);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);

        ...
```
3. **Player hits OR misses**
```csharp
// (MISS DETECTION) only if not being held and not paused
        if (!hasMissed && !isBeingHeld)
        {
            if (newY > missTriggerY)
            {
                TriggerMiss();
            }
        }
```
4. **Visual feedback happens**
```csharp
 void TriggerMissVisuals()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0.3f; 
        foreach (Image img in imagesToColor)
        {
            if (img != null) img.color = Color.gray;
        }
    }
```
5. **Note destroyed off-screen**
```csharp
 // Clean up off-screen notes
        if (!isBeingHeld && newY > startY + 2500f) 
        {
            Destroy(gameObject);
        }
    }
```

But now that we have the note (arrow) object all set and ready we need an actual brain of one of the lanes. If NoteObject.cs is an individual arrow then LaneController is the system that can Detect Player Input, Decided if note was hit, handles scoring, handles hold notes, plays animations and sounds, controls receptor (the target point arrow) visual and tracks notes inside the hit zone. 

**THE OVERALL FLOW OF LANECONTROLLE.CS**
  1. Song stars
  2. Lane spawns notes
  3. Notes move toward receptor
  4. Player presses key
  5. Lane checks closest note distance
  6. Awards the player with the visual feedback
  7. Tracks hold notes
  8. Visuals + score + character animation happen. 

Okay so if NoteObject.cs controlls ONE note and LaneController.cs Controlls ONE lane. We need something that can controll and supervise the entire song/game. For that I have GameplayManager.cs

Gameplay manager acts as a conductor, it checks that the song follows the timeline animations, tells LaneControloler when to spawn the NoteObject.cs. Has Pause system as well as in charge of the Input system for players and Scene Manager as well as Syncing everything that is happening in the game. 

**THE OVERALL FLOW OF GAMEPLAYMANAGE.CS**
1. Load song/chart (the txt file where we specify at what time and what lane the arrows need to be spawned)
```csharp
void CheckSpawns(double currentSongTime)
{
    double lookAheadTime = currentSongTime + spawnOffset;

    while (currentNoteIndex < songChart.Count)
    {
        NoteEvent nextNote = songChart[currentNoteIndex];

        if (lookAheadTime >= nextNote.time)
        {
            float holdDur =
                holdsAreInBeats
                ? nextNote.holdLength * secondsPerBeat
                : nextNote.holdLength;

            SpawnNote(nextNote.laneIndex, holdDur, nextNote.time);

            currentNoteIndex++;
        }
        else break;
    }
}
```
2. Sets up the Input for Player 1 and Player 2
```csharp
SetupPlayerInputs();
```
**Player device pairing**
```csharp
InputUser.PerformPairingWithDevice(
    SessionConfig.Player1Device,
    p1Input.user
);
```
**Load custom bindings**
```csharp
p1Input.actions.LoadBindingOverridesFromJson(
    SessionConfig.P1Bindings
);
```

**Enable input actions**
```
p1Input.actions.Enable();
```
**Give lanes their input actions**
```csharp
p1Left.Initialize(p1Input.actions["Left"]);
p1Down.Initialize(p1Input.actions["Down"]);
p1Up.Initialize(p1Input.actions["Up"]);
p1Right.Initialize(p1Input.actions["Right"]);
```
*(SAME FOR THE PLAYER 2)*

3. Scheduales music perfectly to be alligned with the Timeline animations.
```csharp
if (director != null && songTime >= 0 && !musicStarted)
{
    musicStarted = true;
    director.Play();
}
```
#### songTime = (DSPTime - songStart) - pauseOffset
4. Tracks global state
5. Pause/ Resume logic if triggered
6. Ends level
```csharp
GameSessionData.P1Score =
    p1ScoreManager.currentScore;

GameSessionData.P1MaxCombo =
    p1ScoreManager.maxCombo;
```

These 3 core components that what makes my game work. Each one of them has it's own responsibilities and tasks, they works together as one.

As well as that I have a little script that creates procedural animations for my characters called CharacterAnimatorController.cs

**THE OVERALL FLOW OF CHARACTERANIMATORCONTROLLER.CS**
1. Lane hit detected
2. Triggers aniomations
3. Animation name is generated (Left1, Up3, etc)
4. Added to the queue
5. Couratine processes queue
6. Animator plays each animation fully
7. If no aniamtions are pressent in the couratine return to Idl. 

### Song Selection UI

I needed the system of the main song selection scene. Which would pop the fun and sas of the game from the get go. In prototype I had a simular system as I do in the final I just moved from the 3D CD carousel to 2D song icons carouse. (Figure 9)

<a href="https://ibb.co/WvhqHFXr"><img src="https://i.ibb.co/5h0t1v3Q/4707872691654272595-ezgif-com-video-to-gif-converter.gif" alt="4707872691654272595-ezgif-com-video-to-gif-converter" border="0" /></a>
*Figure 9: Old UI concept.*

<a href="https://ibb.co/XZCH1Ytq"><img src="https://i.ibb.co/ccrq0YQj/IMG-0246.jpg" alt="IMG-0246" border="0" /></a>
*Figure 10: First concept of a new UI*

In thw final I wanted somethig that would scream the vibe of the song not only from the actual demo song but also visually. So I added a dynamic background and character cutout to showcase what player should be expecting from the song, what enviroment, what character whats the vibe of the song before commiting to it. (Figure 11)

<a href="https://ibb.co/N6S3m2F8"><img src="https://i.ibb.co/ynSRYFNw/Screenshot-2026-05-20-at-13-36-54.png" alt="Screenshot-2026-05-20-at-13-36-54" border="0" /></a>
*Figure 111: New Song Selection menu*

The most import for me was to create a smooth carousel like movemnt between the song. (Figure 12)

<a href="https://ibb.co/tM6sMLkB"><img src="https://i.ibb.co/jvYMvJBh/Screen-Recording2026-05-20at13-43-56-ezgif-com-video-to-gif-converter.gif" alt="Screen-Recording2026-05-20at13-43-56-ezgif-com-video-to-gif-converter" border="0" /></a>
*Figure 12: Gif showcasing UI scrollin*

To achieve this I needed 3 core pieces
1. CurrentSongIndex 
2. List<RectTransform> spawnedBlocks
3. RectTransform[] slots;

**(LEFT)-(LEFT-CENTER)-(CENTER)-(RIGHT-CENTER)-(RIGHT)**

*How does input moves the selection?*
Well when Player uses InputActions.ActionMaps.UI NavigationLeft or NavigationRight we modify the index of the current selection therefor moving them arround. For example when player presses NavigationLeft index = index - 1. 
```csharp
void OnMove(int dir)
{
    currentSongIndex = (currentSongIndex + dir + allSongData.Count) % allSongData.Count;

    UpdateSelectionVisuals();
}
```
To make sure it makes a loop so if it goes past last we back to 0 and below 0 we go to last we use 
```
% allSongData.Count

AND

if (rawDiff > spawnedBlocks.Count / 2) rawDiff -= spawnedBlocks.Count;
if (rawDiff <= -spawnedBlocks.Count / 2) rawDiff += spawnedBlocks.Count;
```
The actual movemnt animations are
```csharp
spawnedBlocks[i].position = Vector3.Lerp(
    spawnedBlocks[i].position,
    slots[targetSlotIndex].position,
    Time.deltaTime * lerpSpeed
);
```
So instead of snapping instantly (position = slot.position) we make it smooth with Lerp(current, target, speed) (Unity Technologies, s.d.)

So simplifed version of this code is 
```csharp
for each song:
    position = slot[distanceFromSelected]
    smoothMove(position)
```


### Main Menu and sub menus
I needed the system of main menus and subsystems that would channel the games atmosphere and won't look outside of the avarage design. At the beggining of the development the idea was to make Slay the beat fully arcade system but later on I changed my mind and converted the system and everhing to normal game style. So before hand I had timer system that would auto select if player hasn't made up their mind, it got cut in the final version. The concept UI is channeling a basic idea, but doesnt look polished enough. 
<a href="https://ibb.co/6c1BGJ4r"><img src="https://i.ibb.co/GftTG3Vn/Screen-Recording2026-05-20at15-50-04-ezgif-com-video-to-gif-converter.gif" alt="Screen-Recording2026-05-20at15-50-04-ezgif-com-video-to-gif-converter" border="0" /></a>
*Figure 13: Gif showcasing the old UI concept *

Because i decided to stuck with these blocky style menu I needed to make sure it can be modified to different scales as so I made a systerm that treats buttons as index holders, depending on the index we not only able to move around but also call functions depending on the current active index. 

The big Picture of the ArcadeMainMenu.cs is
1. Game Stars ->
```csharp
void Start()
{
    
    // Set default selection to START button
    index = 2;

    // Reset state flags
    hasSelected = false;
    isFading = true;

    // Reset visuals before fade-in
    SetInitialVisuals();

    // Disable interaction during intro
    canvasGroup.alpha = 0f;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;

    // Begin fade-in
    StartCoroutine(FadeInCanvas());
}
```
2. Main Canvas fades in ->
```csharp
IEnumerator FadeInCanvas()
{
    float alpha = 0f;

    while (alpha < 1f)
    {
        alpha = Mathf.MoveTowards(
            alpha,
            1f,
            fadeSpeed * Time.unscaledDeltaTime
        );

        canvasGroup.alpha = alpha;
        yield return null;
    }

    canvasGroup.alpha = 1f;

    // Enable interaction AFTER fade
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;

    isFading = false;

    UpdateSelectionText();
    UpdateStartButtonObjects();
}
```
3. Index (custom) 2 becomes an active main index
```csharp
private void SetDefaultIndex()
{
    index = 2; // START button
}
```
4. Player navigates the menu using InputAction.ActionMaps.UI left or right ->
```csharp
private void Move(int direction)
{
    if (isFading || hasSelected) return;

    if (Time.unscaledTime - lastMoveTime < moveCooldown)
        return;

    lastMoveTime = Time.unscaledTime;

    index = (index + direction + buttons.Length) % buttons.Length;

    audioSource?.PlayOneShot(switchSound);

    UpdateSelectionText();
    UpdateStartButtonObjects();
}
```
5. UI buttons animate (scale + color + utline pulse) ->
```csharp
void Update()
{
    if (isFading || hasSelected) return;

    pulseTime += Time.unscaledDeltaTime;

    for (int i = 0; i < buttons.Length; i++)
    {
        bool selected = (i == index);

        Vector3 baseScale = originalScales[i];
        Vector3 targetScale = baseScale;

        // SCALE
        float zoom = (i == 2)
            ? startButtonZoomAmount
            : sideButtonZoomAmount;

        if (selected)
            targetScale = baseScale * (1f + zoom);

        buttons[i].localScale = Vector3.Lerp(
            buttons[i].localScale,
            targetScale,
            Time.unscaledDeltaTime * animSpeed
        );

        // COLOR
        if (i < backgrounds.Length && backgrounds[i] != null)
        {
            Color targetColor = selected ? bgWhite : bgGrey;

            backgrounds[i].color = Color.Lerp(
                backgrounds[i].color,
                targetColor,
                Time.unscaledDeltaTime * bgSpeed
            );
        }

        // OUTLINE PULSE
        if (i < outlines.Length && outlines[i] != null)
        {
            if (selected)
            {
                float pulse = (Mathf.Sin(pulseTime * 3.5f) + 1f) * 0.5f;

                outlines[i].color = Color.Lerp(
                    outlineBlack,
                    outlineGold,
                    pulse
                );
            }
            else
            {
                outlines[i].color = outlineBlack;
            }
        }
    }
}
```
6. Updates the next menu text depending on active index ->
```csharp
private void UpdateSelectionText()
{
    if (selectionText == null) return;

    if (selectionNames != null && index < selectionNames.Length)
    {
        selectionText.text = selectionNames[index];
    }
}
```
7. Player presses Select/Start
```csharp
private void SelectCurrentItem()
{
    if (isFading || hasSelected) return;

    hasSelected = true;

    switch (index)
    {
        case 0:
            LoadScene(recordsScene);
            break;

        case 1:
            LoadScene(newsScene);
            break;

        case 2:
            LoadScene(gameplayScene);
            break;

        case 3:
            LoadScene(settingsScene);
            break;

        case 4:
            QuitGame();
            break;
    }
}
```
8. Next Scene load or Game quits. 
```csharp
private void LoadScene(string sceneName)
{
    if (string.IsNullOrEmpty(sceneName)) return;

    if (TransitionManager.Instance != null)
        TransitionManager.Instance.LoadScene(sceneName);
    else
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
}
```
OR
```csharp
private void QuitGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
```

From feedback that I recived I added the system in the top right corner that shows what would happen if player presses this button it added clearity for player. The final UI version showcasing the updates from the feedback and final version. 

<a href="https://ibb.co/rGsVtkX9"><img src="https://i.ibb.co/4ZdHfW0k/6114762149605892245-ezgif-com-video-to-gif-converter.gif" alt="6114762149605892245-ezgif-com-video-to-gif-converter" border="0" /></a>
*Figure 13: Gif showcasing final UI *

### Input mapping 
And the last thing from my development that I would want to talk about is the input mapping for my game. What makes my game different from competitors is that I want to give the player the opportunity to decide how they want to play, with their keyboard, gamepad or 3rd party dance mat. In the first iteration of the game, the input was hard-coded in the game's Input Actions, which brings the problem that different dance mats have different controllers, so, for example, button left can be button 3 on one controller and button 6 on another. That brings the problem of incorrect input, which would make the game not fair and unpleasant to play. 

At the beginning, I hardcoded all possible button options into the game, but that would bring another problem of some buttons acting as 2 buttons, not just one and the player would press up, but that would trigger up and left, which again breaks and issues with the smoothness of the game. 

So my new approach was to dynamically map in every control player would like to play as, for example, a player can hook left and right to the dancemat, another one to the gamepad and another one to the keyboard. This gives the player full freedom in how to play the game, but also makes the game more stable. 

**THE OVERALL FLOW OF DEVICESETUPMENU.CS**
1. Menu Opens/Fades In
2. Waits for any Input device
```csharp
private void OnEnable()
{
    rootCanvasGroup_Internal.alpha = 0f;

    ResetAllVisuals();

    joinAction = new InputAction(
        binding: "/*/<button>",
        type: InputActionType.PassThrough
    );

    joinAction.performed += OnInputDetected;
    joinAction.Enable();

    StartCoroutine(FadeIn(rootCanvasGroup_Internal, 1f));
}
```
3. Looks the device to the player
```csharp
private void StartMappingSequence(InputDevice device)
{
    isMapping = true;

    SessionConfig.SetPlayerDevice(
        playerIndexToAssign,
        device,
        "Custom"
    );

    currentMapStep = 0;

    StartCoroutine(IntroPopUpAndInitialize());
}
```
4. Calibration into animations (UI fades in each calibration sectionn)
```csharp
IEnumerator IntroPopUpAndInitialize()
{
    promptText.text = "CALIBRATION";

    yield return new WaitForSecondsRealtime(introWaitDuration);

    foreach (var animGroup in calibrationAnimations)
    {
        StartCoroutine(
            FadeIn(animGroup.containerToPulse, 1f, introFadeDuration)
        );

        yield return new WaitForSecondsRealtime(0.1f);
    }

    currentMapStep = 0;

    SequenceNextAction();
}
```
5. Prompt Next Direction
6. Arrow pulse to get players attention
7. Checks if player is holding the button down (to make sure it's the right one)
```csharp
IEnumerator VerifyHoldAndCrossFadeRoutine(
    CalibrationAnimsGroup animGroup,
    InputControl control
)
{
    float holdTimer = 0f;

    while (holdTimer < requiredHoldTime)
    {
        bool isStepping = control.IsActuated(0.1f);

        if (isStepping)
        {
            holdTimer += Time.unscaledDeltaTime;

            float progress =
                holdTimer / requiredHoldTime;

            animGroup.backgroundBaseImage.color =
                Color.Lerp(
                    normalBlackColor,
                    lockedWhiteColor,
                    progress
                );
        }

        yield return null;
    }
}
```
8. If succesful (playing a lock animation) if not aka faild hold (Starting Coroutine of a falling)
9. Repeat untill all directions done
10. Finish calibration
```csharp
private void FinishCalibration()
{
    promptText.text =
        "<color=white>CALIBRATION COMPLETE!</color>";

    string overridesJson =
        playerInputToMap.actions
            .SaveBindingOverridesAsJson();

    if (playerIndexToAssign == 0)
        SessionConfig.P1Bindings = overridesJson;
    else
        SessionConfig.P2Bindings = overridesJson;
}
```
11. Transition to the next menu or Game scene

The calibration system now works by listening for any button press from any connected device. Once the player presses a button, the game captures the InputDevice object directly from the Input System and temporarily locks that hardware to the current player slot. This means Player 1 and Player 2 are completely isolated from each other during setup, preventing accidental cross-inputs between dance mats, keyboards, or controllers. After the device is locked, the player is guided through a calibration sequence where they hold each direction one at a time. Instead of storing “Left = Keyboard Arrow” or “Right = Joystick Button 3,” the game saves the exact hardware control path exposed by Unity, such as a HID button path or gamepad d-pad direction.

Once calibration is complete, the bindings are serialized into JSON using Unity’s built-in binding override system. This JSON contains all remapped controls for that specific player and acts as a portable representation of the player’s input configuration. The data is then stored inside SessionConfig and later reloaded by the GameplayManager when gameplay begins. During gameplay, the note lanes no longer care what physical device the player is using; they simply listen for abstract actions like “Left,” “Right,” “Up,” and “Down.” This separation between hardware and gameplay logic makes the system significantly cleaner, easier to scale, and much closer to the architecture used in commercial rhythm games.

Another major advantage of this approach is flexibility. Because the system relies on InputActions and binding overrides rather than hardware-specific code, the game automatically supports keyboards, Xbox controllers, PlayStation controllers, USB dance mats, arcade IO boards, and generic HID devices without requiring additional gameplay rewrites. It also allows instant recalibration and rebinding during runtime, which is essential for arcade-style experiences where players may constantly swap devices. Architecturally, this creates a much more professional pipeline where gameplay systems remain device-agnostic while all hardware complexity is handled during the calibration and pairing phase.

### Conclussion
Overall, the final architecture of the game became heavily modular, where every system has its own clear responsibility while still working together as a single pipeline. NoteObject.cs is responsible only for individual note behaviour, LaneController.cs manages gameplay interaction inside a single lane, while GameplayManager.cs supervises the entire song flow, timing, spawning, pause logic, and player setup. Separating systems this way made the project significantly easier to debug, scale, and expand during development, especially as more mechanics such as hold notes, character animations, UI feedback, and multiplayer support were introduced.

Another major achievement during development was creating a fully dynamic input calibration and device pairing system. Instead of relying on hard-coded controller layouts, the game dynamically captures hardware input and stores custom bindings as JSON through Unity’s Input System. This allowed the project to support keyboards, controllers, USB dance mats, and generic HID devices without rewriting gameplay logic for every hardware type. By separating gameplay actions from physical devices, the system became more stable, flexible, and much closer to how commercial rhythm games handle input management.

Through the development of Slay The Beat, I was able to combine gameplay programming, UI systems, animation pipelines, input architecture, and audio synchronisation into one complete playable experience. The project evolved far beyond the original prototype and became a polished rhythm game focused on accessibility, responsiveness, and strong visual identity, while also giving me practical experience in scalable Unity architecture and real-time gameplay system design.

## Testing 
For testing I used in house testing as well as a feeback form to collect an annonums feedback from the players. First in house testing happened when I only had 2 levels, I was seeking the feedback from early stages. (Figure 14).

<a href="https://ibb.co/V4Ww0Nf"><img src="https://i.ibb.co/7mJkxzs/IMG-0247.png" alt="IMG-0247" border="0" /></a>
*Figure 14: Photo of in house testing * 

Feedback was that the controlls felt a bit to sensative and the game UI nedded polishing which I already improved.

Another testing session was with my friends at the house party. Mood was already up and I checking if the game suits the load party mood enviroment. (Figure 15).

<a href="https://ibb.co/216qzWGb"><img src="https://i.ibb.co/W43nrGmT/IMG-0248.png" alt="IMG-0248" border="0" /></a>
*Figure 15: House party game test session * 

With this session I was able to recive a feedback about notes being of beat from the proffesinal dancer I understood that the issue was in the way I mapped when the arrow should spawn not the actual code. So after a little rework I was able to fix the issues and get the game up to BEAT.

I had an amazing opportunity to showcase my game at GDLX at UCA where around 20 players had a chance to play my game and give me their feedback. (Figure 16). The main issue player had was that the dance mat 2 was triggering left and up buttons at the same time thats when I moved from the hard codded mapping to dynamic mapping to fix this issue. 

<a href="https://ibb.co/Z1M06dFp"><img src="https://i.ibb.co/vvjSx1T6/IMG-9051-2.jpg" alt="IMG-9051-2" border="0" /></a>
*Figure 15 Photo of in house testing * 

Also, another methoud of getting feedback was to create a QR code with a survay for players to do. I put them on all SLAY THE BEAT MERCH. That even if player didn't do it at the time they can do it in their free time. 
<a href="https://ibb.co/0j6YJFjP"><img src="https://i.ibb.co/Gv8CPtvg/Screenshot-2026-05-20-at-17-21-47.png" alt="Screenshot-2026-05-20-at-17-21-47" border="0" /></a>
*Figure 16 Feedback graph *


## Critical Reflection *(Approx. 10–15% of word count)*

Overall, one of the biggest strengths of this project was how much the final game evolved compared to the original prototype. At the beginning, the idea was simply to create a rhythm-based arcade experience inspired by games such as *Dance Dance Revolution* and *StepMania*. However, throughout development, the project became significantly more ambitious and polished than originally expected. The final version successfully combined responsive rhythm gameplay, dynamic UI systems, procedural character animations, multiplayer support, audio synchronisation, and custom device calibration into one cohesive experience. One of the strongest aspects of the final piece was the modular architecture of the codebase. Separating the project into systems such as `NoteObject.cs`, `LaneController.cs`, and `GameplayManager.cs` made the game easier to debug, optimise, and expand as development progressed. This became especially important once more advanced mechanics, such as hold notes, multiplayer input handling, and timeline synchronisation, were introduced.

Another major success was the overall visual identity and presentation of the game. Through research into older arcade rhythm games, I was able to identify what made classic rhythm games memorable while also recognising where modern improvements could be introduced. The final UI system exceeded my expectations because it managed to preserve the energetic “arcade machine” feeling while still looking modern and polished. The dynamic song selection menu, animated transitions, glowing visual effects, and responsive menu movement all helped reinforce the atmosphere of the game. The addition of dynamic backgrounds and character cut-outs for each song also significantly improved the player experience because it visually communicated the tone and energy of the stage before gameplay even began. Feedback from testing sessions confirmed that players found the game visually exciting and engaging, particularly in party environments where the strong colours, movement, and music helped maintain energy within the room.

The gameplay responsiveness and input architecture were also some of the strongest technical achievements of the project. Initially, I underestimated how difficult it would be to support multiple controller types, especially third-party dance mats. During early testing, issues with incorrect or overlapping inputs created unfair gameplay and negatively affected the rhythm accuracy. However, redesigning the system to use Unity’s Input System with dynamic device pairing and JSON-based binding overrides became one of the most successful parts of the final project. Instead of hard-coding controls for every device, the game now allows players to calibrate their own setup regardless of whether they use keyboards, controllers, or dance mats. This solution not only fixed gameplay problems but also made the project feel much closer to a professional commercial rhythm game. The testing process also demonstrated the effectiveness of this approach, as players could quickly understand and configure the controls without requiring developer intervention.

Despite these successes, there were still several aspects of the project that could be improved further. One of the biggest weaknesses during development was scope management. As the project progressed, I continuously added new ideas and systems, including procedural character animations, dynamic backgrounds, multiplayer functionality, and advanced calibration menus. While these additions improved the final experience, they also significantly increased development complexity and time pressure. Because of this, some systems were not refined as much as they could have been. For example, while the UI became visually polished, certain menu transitions and animations could still feel inconsistent at times. Some sections of the interface also lacked accessibility features such as clearer tutorials, visual onboarding, or colourblind-friendly design considerations. If I had more development time, I would focus more heavily on improving user accessibility and refining the onboarding experience for first-time players.

Another area that could be improved is the gameplay content itself. Although the underlying rhythm engine became technically strong, the project currently contains a relatively limited number of songs and gameplay stages. Creating each level required designing custom visuals, generating music, charting note patterns, and balancing gameplay timing manually, which was extremely time-consuming. As a result, a large portion of development time was spent building systems rather than creating content. If I were to continue this project in the future, I would prioritise creating external editing tools or automated workflows for song chart creation. For example, developing a dedicated beatmap editor could drastically speed up level production and make the game more scalable. More time could also allow for additional gameplay mechanics such as difficulty modifiers, online leaderboards, unlockable cosmetics, or cooperative multiplayer modes.

Testing also revealed several technical issues that highlighted areas for improvement. During early public demonstrations, players discovered problems with note timing, input overlap, and dance mat compatibility. Although these issues were eventually resolved, they exposed weaknesses in my original assumptions about hardware reliability and rhythm synchronisation. In future projects, I would introduce structured testing much earlier in development rather than waiting until larger systems were already implemented. Earlier user testing could have identified problems with calibration, readability, and note timing before the systems became deeply integrated into the architecture. Additionally, while using AI-generated music through Suno.AI solved licensing and budget limitations, relying on generated music also created some creative restrictions. Certain tracks lacked the precise structure and rhythmic consistency typically found in professionally produced rhythm game music. With more resources, collaborating with independent musicians or composers could improve the originality and musical quality of the final product.

Overall, this project was highly valuable both technically and creatively. It allowed me to gain practical experience in gameplay programming, UI development, input architecture, animation systems, audio synchronisation, and user testing within a complete game production pipeline. The project exceeded my expectations in terms of polish, responsiveness, and overall presentation, particularly considering the complexity of supporting multiplayer rhythm gameplay with multiple hardware types. At the same time, the development process highlighted the importance of scope control, iterative testing, and scalable content pipelines. If I continued developing *Slay The Beat*, I would focus on expanding gameplay content, improving accessibility, refining visual consistency, and introducing more advanced player progression systems. Despite its limitations, the final product successfully achieved the original goal of creating a modern arcade-inspired rhythm game that feels energetic, competitive, and immersive for players.


## Bibliography

>Technologies, U. (s.d.) Unity - Scripting API: Vector3.Lerp. At: https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Vector3.Lerp.html (Accessed  20/05/2026).

>Switch-JustDance: Benchmarking Whole Body Motion Tracking Controllers Using a Commercial Console Game At: https://arxiv.org/abs/2511.17925 (Accessed  20/05/2026).

>The Secret To Good Rhythm Games (2021) Directed by Mental Checkpoint. At: https://www.youtube.com/watch?v=LTaGouotcF4 (Accessed  20/05/2026).

>Dance Mat Review. At: https://www.weightlossresources.co.uk/exercise/reviews/dance_mat.htm (Accessed  20/05/2026).

>Introduction to Timeline (s.d.) At: https://learn.unity.com (Accessed  20/05/2026).

>Create shaders and visual effects with URP (Unity 6) (s.d.) At: https://unity.com/resources/create-shaders-visual-effects-urp-unity-6 (Accessed  20/05/2026).

>Just Dance 2017 Ui - 2018 Version (2026) Directed by Eduardo. At: https://www.youtube.com/watch?v=ds_Olgnd0A0 (Accessed  20/05/2026).

>Dance Dance Revolution EXTREME Song List (2019) Directed by ShadowPanther. At: https://www.youtube.com/watch?v=QXKAgyaZLeg (Accessed  20/05/2026).
---

## Declared Assets

> The following assets were created of modified with the use of **Gemeni Pro**:
>* ArcadeMainMenu
>* DifficultySelector
>* MenuSelector
>* MenuSelector
>* SongCarousel
>* SongCarouselManager
>* StageSelector
>* TransitionManager
>* DeviceSelectMenu
>* PersistentSingleton
>* CharacterAnimationController
>* GameplayManager
>* LaneController
>* SongGradeData
>* ResultsManager
>* PlayerScoreManager
>* TextChartLoader


> In game music and sound effect was made in Suno.AI Pro

> DeepSeek:
>* Converting SMEDITOR DATA INTO CHAR FILE 
>* Creating A doxygen 

> SMEditor for creating a visual map of arrow that can be exported and converted later https://tillvit.github.io/smeditor/


> Unity particle system used in the game: https://assetstore.unity.com/packages/vfx/particles/particle-pack-127325

> Enviroment used in one of the levels: https://assetstore.unity.com/packages/3d/environments/urban/cartoon-city-free-low-poly-city-3d-models-pack-328170

