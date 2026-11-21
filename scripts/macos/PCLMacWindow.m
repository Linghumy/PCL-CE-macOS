#import <Cocoa/Cocoa.h>

/// Vertically centers AppKit's standard window buttons in PCL's custom title bar.
void PCLMacOSAlignTrafficLights(void *windowHandle, double titleBarHeight)
{
    if (windowHandle == NULL || titleBarHeight <= 0)
        return;

    NSWindow *window = (__bridge NSWindow *)windowHandle;
    NSArray<NSNumber *> *buttonTypes = @[
        @(NSWindowCloseButton),
        @(NSWindowMiniaturizeButton),
        @(NSWindowZoomButton)
    ];

    for (NSNumber *buttonType in buttonTypes) {
        NSButton *button = [window standardWindowButton:(NSWindowButton)buttonType.integerValue];
        NSView *container = button.superview;
        if (button == nil || container == nil)
            continue;

        CGFloat currentCenterFromTop =
            NSMaxY(container.bounds) - NSMidY(button.frame);
        CGFloat targetCenterFromTop = titleBarHeight * 0.5;
        CGFloat delta = targetCenterFromTop - currentCenterFromTop;

        NSRect frame = button.frame;
        frame.origin.y -= delta;
        [button setFrame:frame];
    }
}
