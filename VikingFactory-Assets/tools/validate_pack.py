"""Structural/integrity validation of generated GLBs. Standard Python + numpy.
This is NOT the official Khronos validator or an in-game integration test.
"""
import json,struct,hashlib
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[1]
results=[]
for path in sorted((root/'models').glob('*/*.glb')):
 raw=path.read_bytes();magic,version,total=struct.unpack_from('<4sII',raw)
 assert magic==b'glTF' and version==2 and total==len(raw)
 jl,jt=struct.unpack_from('<I4s',raw,12);assert jt==b'JSON'
 doc=json.loads(raw[20:20+jl]);bl,bt=struct.unpack_from('<I4s',raw,20+jl);assert bt==b'BIN\0'
 binary=raw[28+jl:];assert len(binary)==bl
 names=[n['name'] for n in doc['nodes']];assert len(names)==len(set(names))
 count=0
 for mesh in doc['meshes']:
  for p in mesh['primitives']:
   arrays={}
   for key,ai in p['attributes'].items():
    ac=doc['accessors'][ai];view=doc['bufferViews'][ac['bufferView']];n={'VEC3':3,'VEC2':2}[ac['type']]
    assert view['byteOffset']%4==0 and view['byteOffset']+view['byteLength']<=len(binary)
    ar=np.frombuffer(binary,dtype='<f4',count=ac['count']*n,offset=view['byteOffset']).reshape(-1,n)
    assert np.isfinite(ar).all();arrays[key]=ar
   pos=arrays['POSITION'];norm=arrays['NORMAL'];assert len(pos)%3==0 and len(pos)==len(norm)
   assert np.allclose(np.linalg.norm(norm,axis=1),1,atol=1e-5)
   tris=pos.reshape(-1,3,3);cross=np.cross(tris[:,1]-tris[:,0],tris[:,2]-tris[:,0]);assert (np.linalg.norm(cross,axis=1)>1e-8).all()
   assert (np.einsum('ij,ij->i',cross,norm[::3])>0).all()
   count+=len(tris)
 meta=json.loads((path.parent/'asset.json').read_text());assert count==meta['triangles']
 for marker in meta['markers']:
  assert marker['name'] in names and np.isclose(np.linalg.norm(marker['forward']),1)
 results.append({'asset':path.stem,'triangles':count,'nodes':len(names),'sha256':hashlib.sha256(raw).hexdigest(),'status':'pass'})
assert len(results)==12
report={'checks':'GLB chunks, offsets, finite geometry, triangle area, normal direction/length, node names, markers, metadata counts','not_tested':['Blender execution','FBX conversion','Unity import','Valheim integration','runtime performance'],'assets':results}
(root/'validation-report.json').write_text(json.dumps(report,indent=2));print(json.dumps({'models':len(results),'status':'pass','total_triangles':sum(x['triangles'] for x in results)}))
