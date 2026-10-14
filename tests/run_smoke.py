import subprocess,time,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
exe=root/'Pong/Builds/Windows/Pong.exe'
logs=root/'tests/logs'; logs.mkdir(exist_ok=True)
children=[]
def launch(name,role,extra=()):
    info=subprocess.STARTUPINFO(); info.dwFlags |= subprocess.STARTF_USESHOWWINDOW; info.wShowWindow=0
    p=subprocess.Popen([str(exe),'-batchmode','-nographics',role,'-smoke','-logFile',str(logs/(name+'.log')),*extra],startupinfo=info)
    children.append(p); print(name,p.pid,flush=True); return p
try:
    server=launch('server','-server'); time.sleep(2)
    left=launch('left','-client'); time.sleep(2)
    right=launch('right','-client',('-drop-client',)); time.sleep(2)
    spectator=launch('spectator','-client'); time.sleep(12)
    if right.poll() is None: right.terminate()
    right.wait(timeout=5); print('right disconnected',flush=True)
    time.sleep(5)
    reconnect=launch('reconnect','-client')
    time.sleep(16)
finally:
    for p in children:
        if p.poll() is None: p.terminate()
    for p in children:
        try: p.wait(timeout=5)
        except subprocess.TimeoutExpired: p.kill()
print('smoke logs:',logs,flush=True)
