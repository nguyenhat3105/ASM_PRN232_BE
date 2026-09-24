"""Run ONLY against a disposable test database: python tests/api_integration.py http://localhost:5081."""
import json, sys, uuid
from urllib.request import Request, urlopen
from urllib.error import HTTPError

base = sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5081'
passed = 0
def call(method, path, data=None, expected=200):
    global passed
    request = Request(base + path, data=json.dumps(data).encode() if data is not None else None,
                      headers={'Content-Type': 'application/json'}, method=method)
    try:
        response = urlopen(request)
    except HTTPError as error:
        response = error
    body = response.read()
    assert response.status == expected, (method, path, response.status, body.decode())
    passed += 1
    return json.loads(body) if body else None

suffix = uuid.uuid4().hex[:10]
assert len(call('GET','/api/departments')) >= 5
assert len(call('GET','/api/projects')) >= 6
assert len(call('GET','/api/tasks')) >= 17
call('GET','/swagger/v1/swagger.json')
call('GET','/api/departments/6',expected=404)
call('GET','/api/projects/7',expected=404)
call('GET','/api/tasks/search?status=9',expected=400)
call('POST','/api/departments',{'departmentName':' ','departmentDescription':''},400)
department=call('POST','/api/departments',{'departmentName':'QA '+suffix,'departmentDescription':'Disposable integration test'},201)
did=department['departmentId']
project_body={'projectName':'QA '+suffix,'startDate':'2026-01-01','endDate':'2026-12-31','status':0,'departmentId':did}
call('POST','/api/projects',{**project_body,'endDate':'2025-01-01'},400)
project=call('POST','/api/projects',project_body,201); pid=project['projectId']
call('DELETE',f'/api/departments/{did}',expected=400)
tag1=call('POST','/api/tags',{'tagName':'qa-a-'+suffix,'color':'#ABCDEF'},201)
tag2=call('POST','/api/tags',{'tagName':'qa-b-'+suffix,'color':None},201)
call('POST','/api/tags',{'tagName':'QA-A-'+suffix},400)
call('POST','/api/tags',{'tagName':'bad-'+suffix,'color':'invalid'},400)
task_body={'title':'QA '+suffix,'projectId':pid,'status':0,'priority':0,'dueDate':'2020-01-01','tagIDs':[tag1['tagId'],tag1['tagId']]}
task=call('POST','/api/tasks',task_body,201); tid=task['taskId']
assert task['priority']==0 and len(task['tags'])==1
call('DELETE',f'/api/projects/{pid}',expected=400)
call('DELETE',f"/api/tags/{tag1['tagId']}",expected=400)
found=call('GET',f'/api/tasks/search?title={suffix}&status=0&priority=0&projectId={pid}&tagId={tag1["tagId"]}')
assert [t['taskId'] for t in found]==[tid]
assert call('GET',f'/api/tasks/project/{pid}')[0]['taskId']==tid
assert call('GET',f'/api/projects/department/{did}')[0]['projectId']==pid
call('PUT',f'/api/tasks/{tid}',{**task_body,'tagIDs':[99999999]},400)
assert len(call('GET',f'/api/tasks/{tid}')['tags'])==1
task=call('PUT',f'/api/tasks/{tid}',{**task_body,'tagIDs':[tag2['tagId']]})
assert [t['tagId'] for t in task['tags']]==[tag2['tagId']] and task['modifiedDate']
assert call('GET',f'/api/projects/{pid}')['tasks'][0]['tags'][0]['tagId']==tag2['tagId']
call('DELETE',f"/api/tags/{tag1['tagId']}",expected=204)
call('DELETE',f'/api/tasks/{tid}',expected=204)
call('GET',f'/api/tasks/{tid}',expected=404)
assert any(t['taskId']==tid for t in call('GET','/api/tasks/trash'))
call('DELETE',f'/api/projects/{pid}',expected=400)
call('DELETE',f"/api/tags/{tag2['tagId']}",expected=400)
call('POST',f'/api/tasks/{tid}/restore')
call('POST',f'/api/tasks/{tid}/restore',expected=400)
call('PATCH',f'/api/tasks/{tid}/status',{'status':2})
assert not call('GET',f'/api/tasks/search?title={suffix}&overdue=true')
call('PATCH',f'/api/tasks/{tid}/status',{'status':8},400)
call('PUT',f'/api/tasks/{tid}',{**task_body,'tagIDs':[]})
assert not call('GET',f'/api/tasks/{tid}')['tags']
call('DELETE',f"/api/tags/{tag2['tagId']}",expected=204)
empty=call('POST','/api/projects',{**project_body,'projectName':'Empty'},201)
call('DELETE',f'/api/projects/{empty["projectId"]}',expected=204)
empty_dept=call('POST','/api/departments',{'departmentName':'Empty','departmentDescription':'Delete test'},201)
call('DELETE',f'/api/departments/{empty_dept["departmentId"]}',expected=204)
print(f'PASS: {passed} API checks including relational constraints, atomic tag updates, zero enums, soft delete and restore.')
