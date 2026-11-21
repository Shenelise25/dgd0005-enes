"""Run a real UnityTransport listen host and guest, then check captured evidence."""
import json
import pathlib
import re
import subprocess
import sys
import time

root = pathlib.Path(__file__).resolve().parent.parent
relay = '--relay' in sys.argv
evidence = root / ('Week06/evidence/relay' if relay else 'Week06/evidence')
evidence.mkdir(parents=True, exist_ok=True)
exe = root / 'Pong/Builds/Windows/Pong.exe'
children = []
join_code = None
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
        connection = ['-hw3-relay'] + (['-hw3-relay-code',join_code] if role == 'client' else []) if relay else [f'-{role}']
        children.append(subprocess.Popen([str(exe), *graphics,
            *connection, '-hw3-test', '-logFile', str(log)], startupinfo=info))
        deadline = time.monotonic() + 40
        while not (log.exists() and '[Client] I am client' in log.read_text(encoding='utf-8', errors='replace')):
            if time.monotonic() > deadline:
                raise TimeoutError(f'{role}: did not spawn an owned paddle')
            time.sleep(.2)
        if relay and role == 'host':
            join_code = re.search(r'\[Relay\] Join code: (\w+)',log.read_text(encoding='utf-8',errors='replace'))[1]
    for child in children:
        child.wait(timeout=65 if relay else 45)
finally:
    for child in children:
        if child.poll() is None:
            child.terminate()

logs = {role: (evidence / f'{role}.log').read_text(encoding='utf-8', errors='replace') for role in ['host','client']}
rows = {role: [dict(re.findall(r'(\w+)=([^ ]+)', line)) for line in content.splitlines() if line.startswith('HW3_SAMPLE ')] for role, content in logs.items()}
host = rows['host']
guest = rows['client']
server_guests = [r for r in host if r['side'] == '1']
serve_times = sorted({float(r['serveTime']) for r in server_guests if float(r['serveTime']) >= 0})
host_movement = [r for r in host if r['side'] == '0' and 10 <= float(r['t']) < 10.3]
measured_speeds = [abs(float(b['z']) - float(a['z'])) * 30 / (int(b['tick']) - int(a['tick']))
                   for a, b in zip(host_movement,host_movement[1:]) if int(b['tick']) > int(a['tick'])]
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
    'serve_cooldown_expires_after_one_second': len(serve_times) >= 2 and serve_times[1] - serve_times[0] >= 1 and logs['client'].count('[Client] Flash: TOO FAST') == 1,
    'host_speed_change_affects_motion': bool(measured_speeds) and max(measured_speeds) > 8.1 and max(measured_speeds) <= 40.1,
    'no_runtime_errors': all(not re.search(r'Exception:|NullReferenceException|error CS|Failed to start', content) for content in logs.values()),
}
if '--screenshots' in sys.argv:
    checks['guest_screenshot_captured'] = (evidence / 'guest-host-score.png').exists()
if relay:
    checks = {
        'live_Relay_join_code_created': bool(join_code and len(join_code) == 6),
        'host_LEFT_guest_RIGHT': checks['host_LEFT_guest_RIGHT'],
        'host_score_replicated_via_Relay': checks['host_G_replicated_to_guest'],
        'ball_in_play_on_both_peers': all(any(r['ball'] == 'True' for r in rows[role]) for role in rows),
        'guest_RTT_received': any(int(r['rtt']) > 0 for r in guest),
        'no_runtime_errors': checks['no_runtime_errors'] and all('[Relay] Host failed' not in text and '[Relay] Join failed' not in text for text in logs.values()),
    }
    if '--screenshots' in sys.argv:
        checks['guest_screenshot_captured'] = (evidence / 'guest-host-score.png').exists()
report = {'passed': sum(checks.values()), 'total': len(checks), 'checks': checks,
          'guest_RTT_ms': sorted({int(r['rtt']) for r in guest})}
(evidence / 'test-summary.json').write_text(json.dumps(report,indent=2) + '\n',encoding='utf-8')
for role, content in logs.items():
    (evidence / f'{role}.txt').write_text('\n'.join(line for line in content.splitlines() if line.startswith(('HW3_', '[Relay]', '[Host]', '[Client] Flash:', '[Server] Rejected'))) + '\n',encoding='utf-8')
print(json.dumps(report,indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
