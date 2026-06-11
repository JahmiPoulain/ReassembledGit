#import <UIKit/UIKit.h>

extern "C" {

void _HapticLight() {
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        [g prepare];
        [g impactOccurred];
    }
}

void _HapticMedium() {
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        [g prepare];
        [g impactOccurred];
    }
}

void _HapticSuccess() {
    if (@available(iOS 10.0, *)) {
        UINotificationFeedbackGenerator *g = [UINotificationFeedbackGenerator new];
        [g prepare];
        [g notificationOccurred:UINotificationFeedbackTypeSuccess];
    }
}

void _HapticError() {
    if (@available(iOS 10.0, *)) {
        UINotificationFeedbackGenerator *g = [UINotificationFeedbackGenerator new];
        [g prepare];
        [g notificationOccurred:UINotificationFeedbackTypeError];
    }
}

} // extern "C"
