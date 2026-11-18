import json, subprocess, time, urllib.request, urllib.error
from pathlib import Path
root=Path(__file__).resolve().parent
logs=[]
def request(base, method, path, expected, body=None, content_type='application/json', code=None):
    data=None if body is None else (body.encode() if isinstance(body,str) else json.dumps(body).encode())
    req=urllib.request.Request(base+path,data=data,method=method,headers={'Content-Type':content_type} if data is not None else {})
    try: r=urllib.request.urlopen(req,timeout=5)
    except urllib.error.HTTPError as e: r=e
    raw=r.read().decode(); status=r.status
    assert status==expected,(method,path,status,raw)
    obj=json.loads(raw) if raw and 'application/json' in r.headers.get('Content-Type','') else None
    if code: assert obj['code']==code,obj
    logs.append(f'{method} {path} -> {status}'+(f' {code}' if code else ''))
    return obj,r.headers,raw

def start(project,port):
    dll=root/project/'bin/Debug/net10.0'/f'{project.name}.dll'
    p=subprocess.Popen(['dotnet',str(dll),'--urls',f'http://localhost:{port}'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    for _ in range(100):
        try:
            urllib.request.urlopen(f'http://localhost:{port}/',timeout=.3).close();return p
        except Exception:
            if p.poll() is not None: raise RuntimeError('API failed to start')
            time.sleep(.1)
    p.terminate();raise RuntimeError('API startup timeout')

def client(project,base,label):
    result=subprocess.run(['dotnet',str(root/project/'bin/Debug/net10.0'/f'{project.name}.dll'),base],capture_output=True,text=True,timeout=25)
    assert result.returncode==0,result.stdout+result.stderr
    assert all(f'{i}.' in result.stdout for i in range(1,5)),result.stdout
    logs.append('\n'+label+'\n'+result.stdout)

for kind,folder,port in [('Catalog','CatalogApi',15100),('Submission','RestfulClientServer',15000)]:
    api=Path(folder)/'src'/f'{kind}.Api'; base=f'http://localhost:{port}'
    p=start(api,port)
    try:
        request(base,'GET','/',200)
        if kind=='Catalog':
            request(base,'GET','/api/products',200)
            request(base,'GET','/api/products/1',200)
            request(base,'GET','/api/products/999',404,code='PRODUCT_NOT_FOUND')
            assert request(base,'GET','/api/products/abc',404)[2]==''
            body=json.loads((root/folder/'src/Catalog.Api/sp-hop-le.json').read_text(encoding='utf-8-sig'))
            obj,headers,_=request(base,'POST','/api/products',201,body)
            assert obj['price']==80000
            read,_,_=request(base,'GET',headers['Location'],200); assert read==obj
            request(base,'POST','/api/products',400,{'name':'','price':-1},code='INVALID_PRODUCT')
            request(base,'POST','/api/products',415,body,'application/x-www-form-urlencoded')
        else:
            request(base,'GET','/api/assignments',200)
            request(base,'GET','/api/assignments/1',200)
            request(base,'GET','/api/assignments/999',404,code='ASSIGNMENT_NOT_FOUND')
            assert request(base,'GET','/api/assignments/abc',404)[2]==''
            body={'studentCode':' SV001 ','repositoryUrl':' https://example.com/repo '}
            obj,headers,_=request(base,'POST','/api/assignments/1/submissions',201,body)
            assert obj['studentCode']=='SV001' and obj['status']=='received'
            old_location=headers['Location']
            read,_,_=request(base,'GET',old_location,200);assert read==obj
            request(base,'POST','/api/assignments/1/submissions',400,{'studentCode':'','repositoryUrl':''},code='INVALID_SUBMISSION')
            request(base,'POST','/api/assignments/1/submissions',400,{'studentCode':'SV001'},code='INVALID_SUBMISSION')
            request(base,'POST','/api/assignments/3/submissions',400,body,code='ASSIGNMENT_CLOSED')
            request(base,'POST','/api/assignments/999/submissions',404,body,code='ASSIGNMENT_NOT_FOUND')
            request(base,'POST','/api/assignments/1/submissions',415,body,'application/x-www-form-urlencoded')
            request(base,'POST','/api/assignments/1/submissions',400,'{"studentCode":')
            request(base,'PUT','/api/assignments/1',405)
            request(base,'GET','/api/submissions/00000000-0000-0000-0000-000000000000',404,code='SUBMISSION_NOT_FOUND')
        client(Path(folder)/'src'/f'{kind}.Client',base,kind+' client lan 1')
        client(Path(folder)/'src'/f'{kind}.Client',base,kind+' client lan 2')
    finally:
        p.terminate();p.wait(timeout=10)
    failed=subprocess.run(['dotnet',str(root/folder/'src'/f'{kind}.Client'/'bin/Debug/net10.0'/f'{kind}.Client.dll'),base],capture_output=True,text=True,timeout=25)
    assert failed.returncode==1 and 'Loi ket noi' in failed.stdout
    logs.append(kind+': API tat -> client bao Loi ket noi (exit 1)')
    p=start(api,port)
    try:
        if kind=='Catalog': assert len(request(base,'GET','/api/products',200)[0])==3
        else: request(base,'GET',old_location,404,code='SUBMISSION_NOT_FOUND')
        logs.append(kind+': khoi dong lai -> du lieu RAM duoc reset')
    finally: p.terminate();p.wait(timeout=10)
(root/'verification-results.txt').write_text('\n'.join(logs),encoding='utf-8')
print('\n'.join(logs))
