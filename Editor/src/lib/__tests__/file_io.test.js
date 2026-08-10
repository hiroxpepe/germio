// @vitest-environment jsdom
import { describe, test, expect, vi, beforeEach } from 'vitest';
import { open_file, save_file } from '../file_io.js';

describe('open_file', () => {
    test('uses showOpenFilePicker when the browser supports it', async () => {
        const fake_file = { text: () => Promise.resolve('{"a":1}') };
        const fake_handle = { getFile: () => Promise.resolve(fake_file) };
        window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);

        const result = await open_file();

        expect(window.showOpenFilePicker).toHaveBeenCalledOnce();
        expect(result.text).toBe('{"a":1}');
        expect(result.handle).toBe(fake_handle);
    });

    test('falls back to an <input type="file"> when the API is missing', async () => {
        delete window.showOpenFilePicker;

        const fake_file = {
            text: () => Promise.resolve('{"b":2}'),
        };

        // Fake a user picking a file: build the promise, then act as the
        // browser would once a person chooses a file in the hidden input.
        const result_promise = open_file();
        const input = document.querySelector('input[type="file"]');
        Object.defineProperty(input, 'files', { value: [fake_file], configurable: true });
        input.dispatchEvent(new Event('change'));

        const result = await result_promise;
        expect(result.text).toBe('{"b":2}');
        expect(result.handle).toBe(null);
    });
});

describe('save_file', () => {
    beforeEach(() => {
        delete window.showSaveFilePicker;
    });

    test('writes to the existing handle when one was given, asking for no new picker', async () => {
        const written = [];
        const fake_writable = {
            write: (text) => { written.push(text); return Promise.resolve(); },
            close: () => Promise.resolve(),
        };
        const fake_handle = { createWritable: () => Promise.resolve(fake_writable) };
        window.showSaveFilePicker = vi.fn();

        await save_file('hello', fake_handle);

        expect(written).toEqual(['hello']);
        expect(window.showSaveFilePicker).not.toHaveBeenCalled();
    });

    test('uses showSaveFilePicker when no handle was given and the API exists', async () => {
        const written = [];
        const fake_writable = {
            write: (text) => { written.push(text); return Promise.resolve(); },
            close: () => Promise.resolve(),
        };
        const fake_handle = { createWritable: () => Promise.resolve(fake_writable) };
        window.showSaveFilePicker = vi.fn().mockResolvedValue(fake_handle);

        const returned_handle = await save_file('world', null);

        expect(window.showSaveFilePicker).toHaveBeenCalledOnce();
        expect(written).toEqual(['world']);
        expect(returned_handle).toBe(fake_handle);
    });

    test('falls back to a download link when no handle and no API exist', async () => {
        const click_spy = vi.fn();
        const original_create_element = document.createElement.bind(document);
        vi.spyOn(document, 'createElement').mockImplementation((tag) => {
            const el = original_create_element(tag);
            if (tag === 'a') el.click = click_spy;
            return el;
        });
        // jsdom does not implement Blob URLs itself; only the browser does.
        // A plain stand-in is enough here, since this test only checks that
        // the download link is built and clicked, not the real URL's shape.
        window.URL.createObjectURL = vi.fn().mockReturnValue('blob:fake-url');
        window.URL.revokeObjectURL = vi.fn();

        const returned_handle = await save_file('fallback text', null);

        expect(click_spy).toHaveBeenCalledOnce();
        expect(returned_handle).toBe(null);
    });
});
