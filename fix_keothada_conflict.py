from pathlib import Path
path = Path(r'd:\KLTN\DoAnGame\Assets\Scenes\KeoThaDA.unity')
text = path.read_text(encoding='utf-8')
lines = text.splitlines()
out = []
state = 'normal'
conflict_top = []
conflict_bottom = []
for line in lines:
    if line.startswith('<<<<<<< Updated upstream'):
        state = 'top'
        conflict_top = []
        conflict_bottom = []
        continue
    if line.startswith('=======') and state in ('top', 'bottom'):
        state = 'bottom'
        continue
    if line.startswith('>>>>>>> Stashed changes') and state == 'bottom':
        if len(conflict_top) == 1 and len(conflict_bottom) == 1 and conflict_top[0].strip().startswith('- {fileID:') and conflict_bottom[0].strip().startswith('- {fileID:'):
            entries = {conflict_top[0].strip(), conflict_bottom[0].strip()}
            for entry in sorted(entries):
                out.append(entry)
        else:
            out.extend(conflict_top)
        state = 'normal'
        continue
    if state == 'normal':
        out.append(line)
    elif state == 'top':
        conflict_top.append(line)
    elif state == 'bottom':
        conflict_bottom.append(line)
new_text = '\n'.join(out) + ('\n' if text.endswith('\n') else '')
if '<<<<<<< Updated upstream' in new_text or '>>>>>>> Stashed changes' in new_text or '\n=======\n' in new_text:
    raise SystemError('Conflict markers remain')
path.write_text(new_text, encoding='utf-8')
print('cleaned', path)
