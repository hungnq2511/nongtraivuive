// Native helpers for Nông Trại Vui Vẻ (iOS): haptics, local notifications, App Tracking Transparency.
// Called from C# via [DllImport("__Internal")] in Assets/Farm/Scripts/Systems/Device.cs.
#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

static NSString* FarmStr(const char* s) { return s ? [NSString stringWithUTF8String:s] : @""; }

extern "C" {

void FarmHapticImpact(int style)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackStyle st = style <= 0 ? UIImpactFeedbackStyleLight : (style == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy);
        UIImpactFeedbackGenerator* g = [[UIImpactFeedbackGenerator alloc] initWithStyle:st];
        [g prepare];
        [g impactOccurred];
    });
}

void FarmHapticSuccess()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UINotificationFeedbackGenerator* g = [[UINotificationFeedbackGenerator alloc] init];
        [g prepare];
        [g notificationOccurred:UINotificationFeedbackTypeSuccess];
    });
}

void FarmRequestNotifications()
{
    UNUserNotificationCenter* c = [UNUserNotificationCenter currentNotificationCenter];
    [c requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound | UNAuthorizationOptionBadge)
                     completionHandler:^(BOOL granted, NSError* _Nullable error) {}];
}

void FarmScheduleNotification(const char* ident, const char* title, const char* body, double seconds)
{
    if (seconds < 1) seconds = 1;
    UNMutableNotificationContent* content = [[UNMutableNotificationContent alloc] init];
    content.title = FarmStr(title);
    content.body = FarmStr(body);
    content.sound = [UNNotificationSound defaultSound];
    content.badge = @1;
    UNTimeIntervalNotificationTrigger* trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:seconds repeats:NO];
    UNNotificationRequest* req = [UNNotificationRequest requestWithIdentifier:FarmStr(ident) content:content trigger:trigger];
    [[UNUserNotificationCenter currentNotificationCenter] addNotificationRequest:req withCompletionHandler:nil];
}

void FarmCancelAllNotifications()
{
    UNUserNotificationCenter* c = [UNUserNotificationCenter currentNotificationCenter];
    [c removeAllPendingNotificationRequests];
    [c removeAllDeliveredNotifications];
    dispatch_async(dispatch_get_main_queue(), ^{
        [UIApplication sharedApplication].applicationIconBadgeNumber = 0;
    });
}

// App Tracking Transparency: 0 = not determined, 1 = restricted, 2 = denied, 3 = authorized
typedef void (*FarmATTCallback)(int status);

int FarmATTStatus()
{
    if (@available(iOS 14, *)) return (int)[ATTrackingManager trackingAuthorizationStatus];
    return 3;
}

void FarmATTRequest(FarmATTCallback callback)
{
    if (@available(iOS 14, *))
    {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            dispatch_async(dispatch_get_main_queue(), ^{ if (callback) callback((int)status); });
        }];
    }
    else if (callback) callback(3);
}

}
