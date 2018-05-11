module NotificationCenter {
    export function subscribeTo(subject: string, entityId?: number) {
        console.log(subject);
    }

    export function isSubscribingTo(subject: string, entityId?: number): boolean {
        console.log(subject);
        return false;
    }
}