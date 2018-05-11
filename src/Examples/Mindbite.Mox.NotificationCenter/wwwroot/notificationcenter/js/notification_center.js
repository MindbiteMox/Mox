var NotificationCenter;
(function (NotificationCenter) {
    function subscribeTo(subject, entityId) {
        console.log(subject);
    }
    NotificationCenter.subscribeTo = subscribeTo;
    function isSubscribingTo(subject, entityId) {
        console.log(subject);
        return false;
    }
    NotificationCenter.isSubscribingTo = isSubscribingTo;
})(NotificationCenter || (NotificationCenter = {}));
//# sourceMappingURL=notification_center.js.map