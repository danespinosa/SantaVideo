# Inpaint Items Configuration Guide - Controlling Video Consistency

## Overview
The `inpaint_items` parameter tells Sora which parts of your image should remain **consistent** throughout the video. This is crucial for maintaining scene continuity while allowing animation.

---

## Current Configuration (OPTION 1)
**Keep entire scene at START and END only**

```csharp
var inpaintItems = new[]
{
    { frame_index = 0,  crop_bounds = { 0.0, 0.0, 1.0, 1.0 } },  // First frame
    { frame_index = -1, crop_bounds = { 0.0, 0.0, 1.0, 1.0 } }   // Last frame
};
```

**Effect:**
- Frame 0: Your Christmas tree scene (100% match)
- Frames 1-N: Sora animates freely (Santa appears, moves, places gifts)
- Frame -1: Back to your Christmas tree scene (100% match)

**Pros:** Maximum creative freedom for Sora in the middle  
**Cons:** Background/tree might shift or change during animation

---

## OPTION 2: Keep Background Consistent Throughout
**Constrain the BACKGROUND in middle frames too**

Add a middle frame constraint to keep the tree/room consistent:

```csharp
var inpaintItems = new[]
{
    // First frame - full scene
    { frame_index = 0, crop_bounds = { 0.0, 0.0, 1.0, 1.0 } },
    
    // Middle frame - keep bottom 60% (tree area)
    { 
        frame_index = 50,  // 50% through video
        crop_bounds = { 
            left_fraction = 0.0,
            top_fraction = 0.4,     // Start from 40% down (keep bottom 60%)
            right_fraction = 1.0,
            bottom_fraction = 1.0
        }
    },
    
    // Last frame - full scene
    { frame_index = -1, crop_bounds = { 0.0, 0.0, 1.0, 1.0 } }
};
```

**Effect:**
```
┌─────────────────────┐
│   [SANTA AREA]      │  ← Top 40%: Santa can appear/animate freely
│      ↓ ↑            │
├─────────────────────┤
│   🎄 TREE 🎄        │  ← Bottom 60%: Stays consistent (your image)
│   [FLOOR/GIFTS]     │
└─────────────────────┘
```

**Pros:** Tree and room stay consistent, only Santa moves  
**Cons:** Slightly less creative freedom for Sora

---

## OPTION 3: Keep Tree Area Only
**Constrain just the tree region**

```csharp
var inpaintItems = new[]
{
    // First frame
    { frame_index = 0, crop_bounds = { 0.0, 0.0, 1.0, 1.0 } },
    
    // Middle - keep center area (where tree is)
    { 
        frame_index = 50,
        crop_bounds = { 
            left_fraction = 0.2,    // 20% from left
            top_fraction = 0.3,     // 30% from top
            right_fraction = 0.8,   // 80% from left (60% width)
            bottom_fraction = 0.9   // 90% from top (60% height)
        }
    },
    
    // Last frame
    { frame_index = -1, crop_bounds = { 0.0, 0.0, 1.0, 1.0 } }
};
```

**Effect:**
```
┌─────────────────────┐
│ [FREE AREA]         │
│   ┌─────────┐       │  ← Only this center region
│   │  🎄 TREE│       │     stays consistent
│   └─────────┘       │
│ [FREE AREA]         │
└─────────────────────┘
```

---

## OPTION 4: Multiple Middle Frames (Maximum Consistency)
**Add constraints at multiple points throughout video**

```csharp
var inpaintItems = new[]
{
    { frame_index = 0,  crop_bounds = { 0.0, 0.0, 1.0, 1.0 } },  // Start
    { frame_index = 25, crop_bounds = { 0.0, 0.4, 1.0, 1.0 } },  // 25% through
    { frame_index = 50, crop_bounds = { 0.0, 0.4, 1.0, 1.0 } },  // 50% through
    { frame_index = 75, crop_bounds = { 0.0, 0.4, 1.0, 1.0 } },  // 75% through
    { frame_index = -1, crop_bounds = { 0.0, 0.0, 1.0, 1.0 } }   // End
};
```

**Effect:** Background stays rock-solid throughout entire video  
**Pros:** Maximum consistency, tree never shifts  
**Cons:** Less fluid animation, might feel constrained

---

## Crop Bounds Explanation

```
left_fraction:   0.0 = left edge,   1.0 = right edge
top_fraction:    0.0 = top edge,    1.0 = bottom edge
right_fraction:  0.0 = left edge,   1.0 = right edge
bottom_fraction: 0.0 = top edge,    1.0 = bottom edge
```

**Example:** Keep bottom-right quadrant
```csharp
crop_bounds = {
    left_fraction = 0.5,    // Start from middle horizontally
    top_fraction = 0.5,     // Start from middle vertically
    right_fraction = 1.0,   // To right edge
    bottom_fraction = 1.0   // To bottom edge
}
```

---

## Recommended for Santa Video

### Best Option: OPTION 2 with Middle Frame
This keeps the Christmas tree and room consistent while allowing Santa to animate naturally.

```csharp
var inpaintItems = new[]
{
    // Start frame
    new { frame_index = 0, type = "image", file_name = fileName,
          crop_bounds = new { left_fraction = 0.0, top_fraction = 0.0, 
                              right_fraction = 1.0, bottom_fraction = 1.0 } },
    
    // Middle frame - keep tree/floor area (bottom 65%)
    new { frame_index = 50, type = "image", file_name = fileName,
          crop_bounds = new { left_fraction = 0.0, top_fraction = 0.35, 
                              right_fraction = 1.0, bottom_fraction = 1.0 } },
    
    // End frame
    new { frame_index = -1, type = "image", file_name = fileName,
          crop_bounds = new { left_fraction = 0.0, top_fraction = 0.0, 
                              right_fraction = 1.0, bottom_fraction = 1.0 } }
};
```

This configuration:
✅ Keeps tree and presents area consistent  
✅ Allows Santa to appear in upper area  
✅ Returns to exact same scene at end  
✅ Creates smooth, believable animation

---

## Testing Tips

1. **Start Simple:** Use OPTION 1 (current) first to see how Sora animates
2. **Add Constraints:** If background shifts too much, add middle frame constraints
3. **Adjust Bounds:** Experiment with top_fraction values (0.3, 0.4, 0.5) to find sweet spot
4. **Frame Indices:** Can use specific frame numbers or percentages (0-100)

---

## Frame Index Values

- `0` = First frame
- `25` = 25% through video
- `50` = 50% through video (middle)
- `75` = 75% through video
- `-1` = Last frame
- Specific numbers like `5` = Frame number 5

---

## To Implement in Code

Uncomment the middle frame section in Program.cs (lines ~155-170) and adjust the `top_fraction` value to control how much of the bottom is kept consistent.

**Quick adjustment guide:**
- `top_fraction = 0.3` → Keep bottom 70% (more constraint)
- `top_fraction = 0.4` → Keep bottom 60% (balanced)
- `top_fraction = 0.5` → Keep bottom 50% (less constraint)
