# Writing Standard

> How to write the documents in this project, and in the other projects too.
> This is a shared rule. All projects by this maker are to keep it.
> The words here are kept simple, so that a reader whose first language is not
> English is able to take in the sense with little trouble.

---

## Why

This project is open. Its readers come from all parts of the world, and for
many of them English is not the first language. Simple words let more of them
read the documents with no need for a word list at their side.

Simple words are good for one more reason. When an agent (a language model)
reads these documents to do its work, a small and clear set of words gives it
less room to get the sense wrong. Clear words for the reader are clear words
for the agent.

---

## The base: a simple word set

The writing keeps close to Ogden's Basic English — a small set of about 850
words, made to say much with little. But the set is from 1930, and some of its
words are old or feel strange today. So the set is not kept as law.

**Make it better with good sense.** Keep the words simple, but do not keep a
word only because Ogden had it. If a word is old or strange now, use a common
word of today in its place. A language model is good at this weighing: is the
word simple, is it natural now, is it still true to the sense? Use that help.

The aim is not to obey a 1930 list. The aim is clear, simple, current English.

---

## Rules

1. **Keep sentences short.** One thought to a sentence where possible.
2. **Use common words.** If a longer word and a shorter word say the same
   thing, use the shorter one.
3. **Say things straight.** Put the actor first, then the act. "The tool reads
   the file," not "The file is read by the tool."
4. **Do not use words that only experts know**, unless they are technical terms
   (see below). For everyday sense, everyday words.
5. **When a simple word is old or odd today, use the current common word.**
   This is the refine step. Sense over list.

---

## Technical terms

Some words have no simple form. Words like `commit`, `push`, `repository`,
`CLI`, `LLM` name one exact thing, and to say them in simple words would make
the sense less clear, not more. For these, the simple-word rule is turned off.

But such words are not free to use as one likes. Every technical term used in
any document must be in the technical term reference (`tech_terms.md`). If a
term is not in that reference, do not use it; add it there first. The reference
is the one place where these terms are given their sense. The documents point
to it; they do not give the sense again.

So each document is made of two closed word sets:
the simple word set (this standard), and the technical terms (the reference).
Nothing else. This leaves the reader, and the agent, no room to be in doubt.

---

## Scope

This standard is not only for this project. It is the writing standard for all
of this maker's projects. It is kept here for now, with one working project to
prove it. When it is well tested, it may be moved to its own shared place.
