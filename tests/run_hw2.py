"""Launch an actual server, two players and one spectator, then assert log evidence."""
import json, math, pathlib, re, subprocess, sys, time

root = pathlib.Path(__file__).resolve().parent.parent
exe = root / 'Pong/Builds/Windows/Pong.exe'
evidence = root / 'Week04/evidence'
evidence.mkdir(parents=True, exist_ok=True)
children = []
def wait_for(path, marker):
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline:
        if path.exists() and marker in path.read_text(encoding='utf-8', errors='replace'):
            return
        time.sleep(.2)
    raise TimeoutError(f'{path.name}: did not see {marker}')
try:
    for name, role in ([] if '--check-only' in sys.argv else [('server', '-server'), ('left', '-client'), ('right', '-client'), ('spectator', '-client')]):
        info = subprocess.STARTUPINFO()
        info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        info.wShowWindow = 0
        log_path = evidence / (name + '.log')
        if log_path.exists(): log_path.unlink()
        children.append(subprocess.Popen([str(exe), '-batchmode', '-nographics', role, '-hw2-test',
            '-logFile', str(log_path)], startupinfo=info))
        wait_for(log_path, '[Server] Started' if name == 'server' else '[Client] I am client')
    for process in children:
        process.wait(timeout=40)
finally:
    for process in children:
        if process.poll() is None:
            process.terminate()

logs = {name: (evidence / (name + '.log')).read_text(encoding='utf-8', errors='replace')
        for name in ['server', 'left', 'right', 'spectator']}
def samples(name):
    return [dict(re.findall(r'(\w+)=([^ ]+)', line)) for line in logs[name].splitlines() if line.startswith('HW2_SAMPLE ')]
server = samples('server')
left = [r for r in server if r['side'] == '0']
right = [r for r in server if r['side'] == '1']
spectator = [r for r in server if r['side'] == '2']
results = {}
results['server_two_players_spectator'] = bool(left and right and spectator)
burst_runs = re.findall(r'(?:\[Server\] Rejected input from client 0: too fast\.\n)+', logs['server'])
results['one_C_rejects_exactly_19'] = bool(burst_runs) and max(run.count('too fast.') for run in burst_runs) == 19
results['all_positions_finite_and_clamped'] = bool(server) and all(math.isfinite(float(r['z'])) and abs(float(r['z'])) <= 4.001 for r in server)
results['server_paddles_never_teleport'] = bool(left and right) and all(abs(float(r['x']) - (-7 if r['side'] == '0' else 7)) < .001 for r in left + right)
results['speed_never_exceeds_8_units_per_second'] = bool(left and right) and all(abs(float(b['z']) - float(a['z'])) <= 8 * (int(b['tick']) - int(a['tick'])) / 30 + .012 for rows in [left, right] for a, b in zip(rows, rows[1:]))
results['normal_owner_input_moves_both_paddles'] = all(max(float(r['z']) for r in rows) - min(float(r['z']) for r in rows) > 1 for rows in [left, right])
results['spectator_cannot_move_or_serve'] = bool(spectator) and all(float(r['z']) == 0 and int(r['reject']) == 0 for r in spectator)
burst_time = next((float(r['t']) for r in left if int(r['reject']) >= 19), -100)
near_burst = [r for r in right if abs(float(r['t']) - burst_time) < 1]
results['other_player_not_blocked_by_spam'] = len(near_burst) > 1 and max(float(r['z']) for r in near_burst) - min(float(r['z']) for r in near_burst) > .5
after_teleport = logs['left'].split('HW2_ACTION T x=0.000', 1)
corrected = [dict(re.findall(r'(\w+)=([^ ]+)', line)) for line in after_teleport[-1].splitlines() if line.startswith('HW2_SAMPLE ') and 'side=0 ' in line]
results['client_teleport_was_attempted_and_corrected'] = len(after_teleport) == 2 and bool(corrected) and abs(float(corrected[0]['x']) + 7) < .001
results['X_NaN_infinity_tests_executed'] = all(action in logs['left'] for action in ['HW2_ACTION X 50', 'HW2_ACTION NaN', 'HW2_ACTION negative infinity'])
results['legal_serve_and_goal_still_work'] = any(r['ball'] == 'True' for r in server) and any(int(r['score']) > 0 for r in server)
results['no_runtime_errors'] = all(not re.search(r'Exception:|NullReferenceException|error CS', content) for content in logs.values())
report = {'passed': sum(results.values()), 'total': len(results), 'checks': results,
          'client_RTT_ms': {name: sorted({int(r['rtt']) for r in samples(name)}) for name in ['left','right']}}
(evidence / 'test-summary.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
for name, content in logs.items():
    # Keep only assignment evidence in tracked text files; raw player logs stay ignored.
    (evidence / (name + '.txt')).write_text('\n'.join(line for line in content.splitlines() if line.startswith(('HW2_', '[Server] Rejected input', '[Server] LEFT scored', '[Server] RIGHT scored'))) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
raise SystemExit(0 if all(results.values()) else 1)
