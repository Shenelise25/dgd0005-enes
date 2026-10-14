from pathlib import Path
import re,json
root=Path(__file__).resolve().parent
logs={name:(root/'logs'/f'{name}.log').read_text(errors='replace') for name in ['server','left','right','spectator','reconnect']}
checks={}
checks['no_runtime_exceptions']=all('Exception:' not in s for s in logs.values())
checks['roles']=all(f'local={side}' in logs[name] for name,side in [('left',0),('right',1),('spectator',2),('reconnect',1)])
checks['one_ball_after_spawn']=all(all('balls=1' in l for l in s.splitlines() if l.startswith('SMOKE t=') and 'local=-1' not in l) for n,s in logs.items() if n!='server')
checks['goals_replicated']=all(re.search(r'score=[1-9]\d*:[1-9]\d*',logs[n]) for n in ['server','left','right','spectator','reconnect'])
checks['feed_capped_at_six']=all(not re.search(r'feed=(?:[7-9]|\d{2,})',s) and 'feed=6' in s for s in logs.values())
checks['spectator_no_private_serve_notice']='Flash: YOU SERVE' not in logs['spectator']
checks['both_players_private_notices']=all('Flash: YOU SERVE' in logs[n] for n in ['left','right'])
checks['wrong_serve_rejected']='SMOKE_WRONG_SERVE_SENT' in logs['right'] and any('inplay=False' in l and 'score=0:0' in l and 'local=1' in l for l in logs['right'].splitlines() if l.startswith('SMOKE t='))
checks['graceful_disconnect']='SMOKE_GRACEFUL_DISCONNECT' in logs['right'] and 'left: RIGHT' in logs['server']
checks['late_join_current_state']=any('local=1' in l and 'feed=6' in l and re.search(r'score=[1-9]\d*:[1-9]\d*',l) for l in logs['reconnect'].splitlines() if l.startswith('SMOKE t='))
checks['remote_paddle_position']=all(re.search(r'paddles=.*0:-?3[,\.]\d+;1:-?3[,\.]\d+',logs[n]) for n in ['server','left','right','spectator'])
print(json.dumps(checks,indent=2))
(root/'logs/summary.json').write_text(json.dumps(checks,indent=2)+'\n')
assert all(checks.values()), 'Smoke test failed'
