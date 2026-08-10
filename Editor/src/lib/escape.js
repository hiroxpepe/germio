// Every string that ends up inside an innerHTML template in this
// tool ultimately comes from the loaded germio.json file itself — a
// file a person could have gotten from anywhere. Escaping every one
// of these before they touch innerHTML is what stands between "just
// a text field" and a real XSS hole (a crafted node.name breaking out
// of value="..." to add its own onfocus="..." attribute, say).

export function escape_html(text) {
  return String(text)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}

export function escape_attr(text) {
  return escape_html(text)
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
