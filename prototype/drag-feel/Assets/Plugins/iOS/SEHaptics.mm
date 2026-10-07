// Haptic feedback bridge for the Sort Everything drag-feel prototype.
// Called from Haptics.cs via [DllImport("__Internal")].
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *seLight;
static UIImpactFeedbackGenerator *seMedium;
static UIImpactFeedbackGenerator *seHeavy;
static UISelectionFeedbackGenerator *seTick;

extern "C" void _SEHaptic(int style)
{
    if (seLight == nil) {
        seLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        seMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        seHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        seTick = [[UISelectionFeedbackGenerator alloc] init];
    }
    switch (style) {
        case 0: [seLight impactOccurred]; [seLight prepare]; break;
        case 1: [seMedium impactOccurred]; [seMedium prepare]; break;
        case 2: [seHeavy impactOccurred]; [seHeavy prepare]; break;
        default: [seTick selectionChanged]; [seTick prepare]; break;
    }
}
