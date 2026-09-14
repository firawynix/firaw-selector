function validUrl(value) {
  try { const url = new URL(value); return ['http:', 'https:'].includes(url.protocol) ? url : null; }
  catch { return null; }
}

function wildcard(pattern) {
  const escaped = pattern.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replaceAll('*', '.*');
  return new RegExp(`^${escaped}$`, 'i');
}

function ruleMatches(rule, value) {
  const parsed = validUrl(value);
  if (!parsed || !rule || !rule.pattern) return false;
  const pattern = String(rule.pattern).trim();
  switch (rule.type) {
    case 'domain': {
      const domain = pattern.replace(/^\*\./, '').toLowerCase();
      return parsed.hostname.toLowerCase() === domain || parsed.hostname.toLowerCase().endsWith(`.${domain}`);
    }
    case 'exact': return wildcard(pattern).test(value);
    case 'regex': try { return new RegExp(pattern, 'i').test(value); } catch { return false; }
    default: return value.toLowerCase().includes(pattern.toLowerCase());
  }
}

function decide(config, value) {
  const rule = (config.rules || []).find((candidate) => ruleMatches(candidate, value));
  return rule?.browserId || null;
}

function shellQuote(value) { return `'${String(value).replaceAll("'", "'\\''")}'`; }

module.exports = { validUrl, ruleMatches, decide, shellQuote };
