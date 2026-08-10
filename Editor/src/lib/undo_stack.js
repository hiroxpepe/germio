// A plain undo stack of full-file JSON snapshots, one push per change
// made in the editor. Kept as a simple index into an array (not a
// pair of stacks) so pushing a fresh change after an undo naturally
// drops whatever "future" was undone away from, matching how undo
// works in most everyday editors.

export function create_undo_stack() {
    const snapshots = [];
    let position = -1;

    return {
        push(snapshot) {
            snapshots.length = position + 1;
            snapshots.push(snapshot);
            position = snapshots.length - 1;
        },
        can_undo() {
            return position > 0;
        },
        undo() {
            position -= 1;
            return snapshots[position];
        },
    };
}
