// Browser file I/O, with the File System Access API used when the
// browser has it, and a plain fallback (<input type="file"> for open,
// a download link for save) when it does not. This is the one file in
// the tool that must touch real browser objects (window, document);
// everything else stays pure and lives in its own file so it can be
// tested without any of this.

/**
 * Opens a file and reads it as text. Returns { text, handle }. handle
 * is the FileSystemFileHandle when the browser supports the File
 * System Access API (so a later save_file call can write straight
 * back to the same file), or null under the <input type="file">
 * fallback (a plain File object cannot be written back to).
 */
export async function open_file() {
    if (window.showOpenFilePicker) {
        const [handle] = await window.showOpenFilePicker();
        const file = await handle.getFile();
        const text = await file.text();
        return { text, handle };
    }

    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'application/json';
        input.style.display = 'none';
        document.body.appendChild(input);
        input.addEventListener('change', async () => {
            const file = input.files[0];
            const text = await file.text();
            document.body.removeChild(input);
            resolve({ text, handle: null });
        });
        input.click();
    });
}

/**
 * Saves text to a file. If an existing handle is passed (from an
 * earlier open_file call), writes straight back to it and returns
 * that same handle. Otherwise, uses showSaveFilePicker when the
 * browser supports it (returning the new handle for next time), or
 * falls back to a plain download link (returning null, since a
 * downloaded file has no handle to write back to later).
 */
export async function save_file(text, handle) {
    if (handle) {
        const writable = await handle.createWritable();
        await writable.write(text);
        await writable.close();
        return handle;
    }

    if (window.showSaveFilePicker) {
        const new_handle = await window.showSaveFilePicker();
        const writable = await new_handle.createWritable();
        await writable.write(text);
        await writable.close();
        return new_handle;
    }

    const blob = new Blob([text], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'germio.json';
    a.click();
    URL.revokeObjectURL(url);
    return null;
}
