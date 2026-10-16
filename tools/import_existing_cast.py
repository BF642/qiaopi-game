"""Blender: bind an existing Meshy original to the game's stock motion skeleton.

Run with Blender --background --python this_file -- PROJECT ROLE SOURCE_GLB.
Keeps original source bytes, geometry, UVs and native texture pixels. No remesh/LOD.
"""
import sys, json, struct, shutil
from pathlib import Path
import bpy
from mathutils import Vector

project, role, source = sys.argv[sys.argv.index('--') + 1:]
root, source = Path(project), Path(source)
folder = root / 'Assets/Resources/Models' / (role + 'MeshyMotion')
original = root / '美术源文件/人物原稿' / role / 'Original.glb'
folder.mkdir(parents=True, exist_ok=True)
original.parent.mkdir(parents=True, exist_ok=True)
if not original.exists():
    shutil.copy2(source, original)

# Copy embedded images exactly; no re-encoding or resizing.
raw = original.read_bytes()
json_size = struct.unpack_from('<I', raw, 12)[0]
gltf = json.loads(raw[20:20 + json_size])
buffer = raw[28 + json_size:]
material = gltf['materials'][0]
texture_slots = {
    'BaseColor': material['pbrMetallicRoughness']['baseColorTexture']['index'],
    'NativeMetalRough': material['pbrMetallicRoughness']['metallicRoughnessTexture']['index'],
    'Normal': material['normalTexture']['index'],
}
for name, index in texture_slots.items():
    image = gltf['images'][gltf['textures'][index]['source']]
    view = gltf['bufferViews'][image['bufferView']]
    start = view.get('byteOffset', 0)
    ext = '.jpg' if image['mimeType'] == 'image/jpeg' else '.png'
    (folder / (name + ext)).write_bytes(buffer[start:start + view['byteLength']])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root / 'Assets/Resources/Models/WorkerMeshyMotion/WorkerMeshyMotion.fbx'))
armature = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
donor = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o.vertex_groups)
armature.animation_data_clear()
armature.data.pose_position = 'REST'
for bone in armature.pose.bones:
    bone.matrix_basis.identity()
bpy.context.view_layer.update()
donor_points = [donor.matrix_world @ Vector(p) for p in donor.bound_box]
donor_min = min(p.z for p in donor_points)
donor_height = max(p.z for p in donor_points) - donor_min
before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=str(original), merge_vertices=False)
imported = set(bpy.data.objects) - before
meshes = [o for o in imported if o.type == 'MESH']
points = [o.matrix_world @ Vector(p) for o in meshes for p in o.bound_box]
source_min = min(p.z for p in points)
scale = donor_height / (max(p.z for p in points) - source_min)
center = Vector(((min(p.x for p in points) + max(p.x for p in points)) / 2,
                 (min(p.y for p in points) + max(p.y for p in points)) / 2, 0))
for mesh in meshes:
    world = mesh.matrix_world.copy()
    mesh.parent = None
    mesh.matrix_world.identity()
    for vertex in mesh.data.vertices:
        p = world @ vertex.co
        p -= center
        p.z -= source_min
        p *= scale
        p.z += donor_min
        vertex.co = p
    for group in donor.vertex_groups:
        mesh.vertex_groups.new(name=group.name)
    bpy.ops.object.select_all(action='DESELECT')
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    transfer = mesh.modifiers.new('Existing stock skeleton weights', 'DATA_TRANSFER')
    transfer.object = donor
    transfer.use_vert_data = True
    transfer.data_types_verts = {'VGROUP_WEIGHTS'}
    transfer.vert_mapping = 'POLYINTERP_NEAREST'
    transfer.layers_vgroup_select_src = 'ALL'
    transfer.layers_vgroup_select_dst = 'NAME'
    bpy.ops.object.modifier_apply(modifier=transfer.name)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    mesh.parent = armature
    rig = mesh.modifiers.new('Stock Meshy skeleton', 'ARMATURE')
    rig.object = armature
    mesh.name = role + '_OriginalSkin'

# Export only the new skin and the stock skeleton, with its unchanged bone paths.
bpy.ops.object.select_all(action='DESELECT')
armature.select_set(True)
for mesh in meshes:
    mesh.select_set(True)
bpy.context.view_layer.objects.active = armature
output = folder / (role + 'MeshyMotion.fbx')
bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True,
    object_types={'MESH', 'ARMATURE'}, use_mesh_modifiers=False,
    add_leaf_bones=False, bake_anim=False, mesh_smooth_type='OFF',
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False,
    path_mode='STRIP', embed_textures=False)
print('EXISTING_CAST_CONVERTED', role, str(output), flush=True)
