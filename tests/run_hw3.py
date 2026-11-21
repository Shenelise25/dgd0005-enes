"""Run a real UnityTransport listen host and guest, then check captured evidence."""
import json
import pathlib
import re
import subprocess
import sys
import time

root = pathlib.Path(__file__).resolve().parent.parent
evidence = root / 'Week06/evidence'
evidence.mkdir(parents=True, exist_ok=True)
exe = root / 'Pong/Builds/Windows/Pong.exe'
children = []
if '--screenshots' in sys.argv:
    (evidence / 'guest-host-score.png').unlink(missing_ok=True)
try:
    for role in ['host', 'client']:
        log = evidence / f'{role}.log'
        log.unlink(missing_ok=True)
        info = subprocess.STARTUPINFO()
        info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        info.wShowWindow = 1 if '--screenshots' in sys.argv else 0
        graphics = ['-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0', '-hw3-screenshot', str(evidence / 'guest-host-score.png')] if '--screenshots' in sys.argv else ['-batchmode', '-nographics']
        children.append(subprocess.Popen([str(exe), *graphics,
            f'-{role}', '-hw3-test', '-logFile', str(log)], startupinfo=info))
        deadline = time.monotonic() + 40
        while not (log.exists() and '[Client] I am client' in log.read_text(encoding='utf-8', errors='replace')):
            if time.monotonic() > deadline:
                raise TimeoutError(f'{role}: did not spawn an owned paddle')
            time.sleep(.2)
    for child in children:
        child.wait(timeout=45)
finally:
    for child in children:
        if child.poll() is None:
            child.terminate()

logs = {role: (evidence / f'{role}.log').read_text(encoding='utf-8', errors='replace') for role in ['host','client']}
rows = {role: [dict(re.findall(r'(\w+)=([^ ]+)', line)) for line in content.splitlines() if line.startswith('HW3_SAMPLE ')] for role, content in logs.items()}
host = rows['host']
guest = rows['client']
checks = {
    'host_LEFT_guest_RIGHT': any(r['side'] == '0' for r in host) and any(r['side'] == '1' for r in guest),
    'guest_G_does_not_change_host_score': any(2.5 < float(r['t']) < 3 and r['score'] == '0' for r in host),
    'guest_serve_refusal_is_targeted': '[Client] Flash: TOO FAST' in logs['client'] and '[Client] Flash: TOO FAST' not in logs['host'],
    'other_player_can_serve': 'HW3_ACTION host serve' in logs['host'] and any(r['ball'] == 'True' for r in host),
    'guest_T_does_not_change_server_position': all(abs(float(r['x']) - 7) < .01 for r in host if r['side'] == '1'),
    'guest_T_is_corrected': any(float(r['t']) > 4.5 and r['side'] == '1' and abs(float(r['x']) - 7) < .01 for r in guest),
    'C_rejected_on_host_and_guest': all('[Server] Rejected input from client ' + i in logs['host'] for i in {r['id'] for r in host}),
    'host_T_replicated_to_guest': any(r['side'] == '0' and float(r['t']) > 6 and abs(float(r['x']) + 7) > 1 for r in guest),
    'host_G_replicated_to_guest': any(r['score'] == '9' for r in guest),
    'all_cheat_keys_exercised': all('HW3_ACTION ' + key in logs[role] for role in logs for key in ['C','X','T','G']),
    'host_speed_40_exercised': 'HW3_ACTION host speed=40' in logs['host'],
    'no_runtime_errors': all(not re.search(r'Exception:|NullReferenceException|error CS|Failed to start', content) for content in logs.values()),
}
if '--screenshots' in sys.argv:
    checks['guest_screenshot_captured'] = (evidence / 'guest-host-score.png').exists()
report = {'passed': sum(checks.values()), 'total': len(checks), 'checks': checks,
          'guest_RTT_ms': sorted({int(r['rtt']) for r in guest})}
(evidence / 'test-summary.json').write_text(json.dumps(report,indent=2) + '\n',encoding='utf-8')
for role, content in logs.items():
    (evidence / f'{role}.txt').write_text('\n'.join(line for line in content.splitlines() if line.startswith(('HW3_', '[Host]', '[Client] Flash:', '[Server] Rejected'))) + '\n',encoding='utf-8')
print(json.dumps(report,indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
