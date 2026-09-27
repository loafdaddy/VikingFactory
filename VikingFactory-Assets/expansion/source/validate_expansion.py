from pathlib import Path
import json,struct,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
EXPECTED='vf_rope_4m vf_reversing_cog vf_merger vf_iron_belt_2m vf_iron_feeder vf_timber_saw vf_coppice_bed vf_planter vf_harvester vf_steam_engine vf_water_intake vf_sail_wheel vf_flywheel vf_governor vf_finite_mining_head vf_deep_extractor vf_advanced_assembler vf_farm_gantry vf_eitr_motor vf_cooking_tender vf_oven_extension vf_livestock_feed_gate vf_livestock_culling_gate vf_fishing_winch vf_reinforced_steam_engine vf_forage_bed'.split()
ALLOWED={'Oak','OakLight','Iron','Bronze','Hide','Stone'}
results=[]
def parse(p):
 raw=p.read_bytes();magic,ver,length=struct.unpack_from('<4sII',raw);assert magic==b'glTF' and ver==2 and length==len(raw)
 jl,jt=struct.unpack_from('<I4s',raw,12);assert jt==b'JSON';d=json.loads(raw[20:20+jl]);bl,bt=struct.unpack_from('<I4s',raw,20+jl);assert bt==b'BIN\0';binary=raw[28+jl:];assert bl==len(binary)
 return d,binary
for name in EXPECTED:
 p=ROOT/'models'/name/(name+'.glb');d,binary=parse(p);meta=json.loads(p.with_suffix('.json').read_text())
 assert not any(x in d for x in ['textures','images','extensionsUsed','extensionsRequired'])
 assert {m['name'] for m in d['materials']}==ALLOWED
 for n in d['nodes']:assert n.get('scale',[1,1,1])==[1,1,1]
 root=d['nodes'][d['scenes'][0]['nodes'][0]];assert root['name']==name and 'mesh' not in root and root['extras']['material_profile']=='valheim'
 children=[d['nodes'][i] for i in root['children']];static=next(n for n in children if n['name']=='Static');assert 'mesh' not in static and static['translation']==[0,0,0]
 names={n['name']:n for n in children};assert len(names)==len(children)
 for part in meta['parts']:
  n=names[part['name']];assert 'mesh' not in n
  if part['name']!='Static':assert n['extras']['rotation_axis'] in ['x','y','z']
 for marker in meta['markers']:
  n=names[marker['name']];assert 'rotation' not in n and 'matrix' not in n and 'mesh' not in n
  assert np.isclose(np.linalg.norm(marker['forward']),1)
 count=0
 for mesh in d['meshes']:
  assert len(mesh['primitives'])==1
  for prim in mesh['primitives']:
   arr={}
   for semantic,ai in prim['attributes'].items():
    ac=d['accessors'][ai];view=d['bufferViews'][ac['bufferView']];dim={'VEC2':2,'VEC3':3}[ac['type']];assert view['byteOffset']%4==0
    assert view['byteOffset']+view['byteLength']<=len(binary)
    x=np.frombuffer(binary,dtype='<f4',count=ac['count']*dim,offset=view['byteOffset']).reshape(-1,dim);assert np.isfinite(x).all();arr[semantic]=x
   assert len(arr['POSITION'])==len(arr['NORMAL'])==len(arr['TEXCOORD_0'])
   v=arr['POSITION'].reshape(-1,3,3);normal=np.cross(v[:,1]-v[:,0],v[:,2]-v[:,0]);assert (np.linalg.norm(normal,axis=1)>1e-8).all();assert np.allclose(np.linalg.norm(arr['NORMAL'],axis=1),1,atol=1e-5)
   assert (np.einsum('ij,ij->i',normal,arr['NORMAL'][::3])>0).all();count+=len(v)
 assert count==meta['triangles'] and count<3000
 assert abs(meta['bounds'][0][1])<1e-7,(name,meta['bounds'])
 for c in meta['box_colliders']:assert c['parent']=='Static' and all(v>0 for v in c['size'])
 for n in static.get('children',[]):
  node=d['nodes'][n]
  if node['name'].startswith('Collider_'):assert 'mesh' not in node and node['extras']['collider']=='box'
 results.append({'id':name,'triangles':count,'bounds':meta['bounds'],'status':'pass','sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
combined,_=parse(ROOT/'VikingFactory-Expansion-All.glb');assert len(combined['scenes'][0]['nodes'])==26
assert [combined['nodes'][i]['name'] for i in combined['scenes'][0]['nodes']]==EXPECTED
report={'models':26,'status':'pass','total_triangles':sum(r['triangles'] for r in results),'maximum_triangles':max(r['triangles'] for r in results),'checks':['requested IDs and combined scene roots','GLB chunk layout and buffer bounds','no textures/images/compression extensions','material name allowlist','root and Static empty-node hierarchy','identity scales, root-level markers with position only','moving metadata, static box collider metadata','finite geometry, nondegenerate triangles and normalized matching normals','per-piece triangle cap and feet at y=0'],'not_tested':['official Khronos validator','Blender/Unity import','native material lookup','game placement, simulation or multiplayer'],'assets':results}
(ROOT/'Validation-Report.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:v for k,v in report.items() if k in ['models','status','total_triangles','maximum_triangles']}))
