#import <UIKit/UIKit.h>

extern "C" {

void _ShareTextAndImage(const char* text, const char* imagePath)
{
    NSString *shareText = [NSString stringWithUTF8String:text];
    NSMutableArray *items = [NSMutableArray array];

    if (shareText.length > 0)
        [items addObject:shareText];

    if (imagePath != NULL)
    {
        NSString *path = [NSString stringWithUTF8String:imagePath];
        UIImage *image = [UIImage imageWithContentsOfFile:path];
        if (image != nil)
            [items addObject:image];
    }

    if (items.count == 0) return;

    UIActivityViewController *activityVC =
        [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];

    UIViewController *rootVC = [UIApplication sharedApplication].keyWindow.rootViewController;

    // iPad popover support
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad)
    {
        activityVC.popoverPresentationController.sourceView = rootVC.view;
        CGRect center = CGRectMake(rootVC.view.bounds.size.width / 2,
                                   rootVC.view.bounds.size.height / 2, 0, 0);
        activityVC.popoverPresentationController.sourceRect = center;
        activityVC.popoverPresentationController.permittedArrowDirections = 0;
    }

    [rootVC presentViewController:activityVC animated:YES completion:nil];
}

} // extern "C"
