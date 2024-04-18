﻿namespace OpenSilver.ControlsKit.Edit {
    public enum KeyNavigateAction {
        None, Up, 
        // Enter functions as "Down"
        Down, 
        Prev, Next,
        NextOrDown, PrevOrUp,
        Escape, 
        
        // useful for checkbox
        Toggle,
    }
}