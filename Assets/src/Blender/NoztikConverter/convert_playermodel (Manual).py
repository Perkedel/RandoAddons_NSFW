# ======================================================
# Script made by Noztik
# Modified by 乃ㄖ乃爪卂匚 Ü

# How to use?
# 1. Parent all the meshes to a single skeleton and put them into the Input collection
# 2. Then press the "Run Script" button in the top middle of this window
# 3. This will automatically rig your meshes to the new skeleton and export it as a .fbx file into the root folder

# Main functions
# 1. Get the armatures and meshes from the Input collection
# 2. Handle the different number of possible spine bones
# 3. Rename Input armature's bones and vertex groups to Human Citizen bones and vertex groups
# 4. Create metacarpal bones in Input armature
# 5. Create missing joint helper bones in Input armature
# 6. Create twist bones in arms and legs in Input armature
# 7. Create any missing bones in the Human Citizen armature
# 8. Move the Human Citizen armature's bones to match the Input armature in edit mode
# 9. Move the Input meshes to the Human Citizen armature
# 10. Weight paint the metacarpals in the Human Citizen armature
# 11. Pose the Human Citizen armature to match the reference armature shape
# 12. Realign miscellaneous bones in the Human Citizen armature
# 13. Export the Human Citizen armature and meshes
# 14. Cleanup

# Modifications
# 1. Added shapekey preservation when applying modifiers.
# 2. Cleanup bone mapping table with fancier one.
# 3. Some random error fixes

# FBX Export Instructions
	# Primary Bone Axis: X Axis
	# Secondary Bone Axis: Z Axis
	# Add Leaf Bones: False

# ======================================================
# enable moving eyes
EYES_ENABLED = False

# ======================================================
# Import dependencies
import bpy
import mathutils
import math
import os

# ======================================================
bone_rename_table = {
    # Core
    "Hips": "pelvis",
    "Spine": "spine_0",
    "Spine1": "spine_1",
    # "Spine2": "spine_2",
    "Neck": "neck_0",
    "Head": "head",
    # "Eye_L": "eye_L",
    # "Eye_R": "eye_R",

    # Left arm
# ======================================================
    "LeftShoulder": "clavicle_L",
    "LeftArm": "arm_upper_L",
    "LeftForeArm": "arm_lower_L",
    "LeftHand": "hand_L",
    "LeftHandThumb1": "finger_thumb_0_L",
    "LeftHandThumb2": "finger_thumb_1_L",
    "LeftHandThumb3": "finger_thumb_2_L",
    "LeftHandIndex1": "finger_index_0_L",
    "LeftHandIndex2": "finger_index_1_L",
    "LeftHandIndex3": "finger_index_2_L",
    "LeftHandMiddle1": "finger_middle_0_L",
    "LeftHandMiddle2": "finger_middle_1_L",
    "LeftHandMiddle3": "finger_middle_2_L",
    "LeftHandRing1": "finger_ring_0_L",
    "LeftHandRing2": "finger_ring_1_L",
    "LeftHandRing3": "finger_ring_2_L",
    "LeftHandPinky1": "finger_pinky_0_L",
    "LeftHandPinky2": "finger_pinky_1_L",
    "LeftHandPinky3": "finger_pinky_2_L",

	# "LeftHandIndex_Meta": "finger_index_meta_L",
	# "LeftHandMiddle_Meta": "finger_middle_meta_L",
	# "LeftHandRing_Meta": "finger_ring_meta_L",
	# "LeftHandPinky_Meta": "finger_pinky_meta_L",

# ======================================================

    # Right arm
    "RightShoulder": "clavicle_R",
    "RightArm": "arm_upper_R",
    "RightForeArm": "arm_lower_R",
    "RightHand": "hand_R",
    "RightHandThumb1": "finger_thumb_0_R",
    "RightHandThumb2": "finger_thumb_1_R",
    "RightHandThumb3": "finger_thumb_2_R",
    "RightHandIndex1": "finger_index_0_R",
    "RightHandIndex2": "finger_index_1_R",
    "RightHandIndex3": "finger_index_2_R",
    "RightHandMiddle1": "finger_middle_0_R",
    "RightHandMiddle2": "finger_middle_1_R",
    "RightHandMiddle3": "finger_middle_2_R",
    "RightHandRing1": "finger_ring_0_R",
    "RightHandRing2": "finger_ring_1_R",
    "RightHandRing3": "finger_ring_2_R",
    "RightHandPinky1": "finger_pinky_0_R",
    "RightHandPinky2": "finger_pinky_1_R",
    "RightHandPinky3": "finger_pinky_2_R",

	# "RightHandIndex_Meta": "finger_index_meta_R",
	# "RightHandMiddle_Meta": "finger_middle_meta_R",
	# "RightHandRing_Meta": "finger_ring_meta_R",
	# "RightHandPinky_Meta": "finger_pinky_meta_R",

# ======================================================

    # Left leg
# ======================================================
    "LeftUpLeg": "leg_upper_L",
    "LeftLeg": "leg_lower_L",
    "LeftFoot": "ankle_L",
    "LeftToeBase": "ball_L",

    # Right leg
    "RightUpLeg": "leg_upper_R",
    "RightLeg": "leg_lower_R",
    "RightFoot": "ankle_R",
    "RightToeBase": "ball_R",
}
# ======================================================
	# Protected bones
protected_bones = [
	"root_IK",
	"aim_matrix_01",
	"aim_matrixa",
	"aim_matrixb",
	"foot_R_IK_target",
	"foot_L_IK_target",
	"hand_R_IK_attach",
	"hand_R_IK_target",
	"hand_L_IK_attach",
	"hand_L_IK_target",
	"hand_R_to_L_ikrule",
	"hand_L_to_R_ikrule",
	"hold_L",
	"hold_R",
]
# ======================================================

def getArmatureModifier(mesh):
	for mod in mesh.modifiers:
		if mod.type == "ARMATURE":
			return mod
	return None


def applyModifiersWithShapeKeys(mesh, selected_modifiers):
	# Shapekey preservation when applying modifiers
	def disable_modifiers(obj, selected_modifiers):
		changed_modifiers = []
		for modifier in obj.modifiers:
			if modifier.name not in selected_modifiers and modifier.show_viewport:
				changed_modifiers.append(modifier.name)
				modifier.show_viewport = False
		return changed_modifiers

	def evaluate_mesh(obj):
		depsgraph = bpy.context.evaluated_depsgraph_get()
		eval_obj = obj.evaluated_get(depsgraph)
		try:
			return bpy.data.meshes.new_from_object(
				eval_obj,
				preserve_all_data_layers=True,
				depsgraph=depsgraph,
			)
		except TypeError:
			return bpy.data.meshes.new_from_object(
				eval_obj,
				preserve_all_data_layers=True,
			)

	def apply_modifier_to_object(obj, selected_modifiers):
		bpy.context.view_layer.objects.active = obj
		changed_modifiers = disable_modifiers(obj, selected_modifiers)

		for modifier_name in selected_modifiers:
			modifier = obj.modifiers.get(modifier_name)
			if modifier:
				modifier.show_viewport = True

		new_mesh = evaluate_mesh(obj)
		old_mesh = obj.data
		old_mesh_name = old_mesh.name
		obj.data = new_mesh

		for modifier_name in selected_modifiers:
			modifier = obj.modifiers.get(modifier_name)
			if modifier:
				obj.modifiers.remove(modifier)

		if old_mesh.users == 0:
			bpy.data.meshes.remove(old_mesh)
		obj.data.name = old_mesh_name

		for modifier_name in changed_modifiers:
			modifier = obj.modifiers.get(modifier_name)
			if modifier:
				modifier.show_viewport = True

	def duplicate_object(obj):
		new_obj = obj.copy()
		new_obj.data = obj.data.copy()
		target_collection = obj.users_collection[0] if obj.users_collection else bpy.context.scene.collection
		target_collection.objects.link(new_obj)
		return new_obj

	def save_shape_key_properties(obj):
		properties_dict = {}
		for idx, key_block in enumerate(obj.data.shape_keys.key_blocks):
			if idx == 0:
				continue
			properties_object = {p.identifier: getattr(key_block, p.identifier) for p in key_block.bl_rna.properties if not p.is_readonly}
			properties_dict[key_block.name] = {"properties": properties_object}
		return properties_dict

	def restore_shape_key_properties(obj, property_dict):
		for key_name, key_data in property_dict.items():
			key_block = obj.data.shape_keys.key_blocks.get(key_name)
			if not key_block:
				continue
			for prop, value in key_data["properties"].items():
				try:
					setattr(key_block, prop, value)
				except Exception:
					continue

	def save_shape_key_drivers(obj):
		import re

		drivers = {}
		shape_keys = obj.data.shape_keys
		if not shape_keys or not shape_keys.animation_data:
			return drivers

		for driver in shape_keys.animation_data.drivers:
			data_path_parts = driver.data_path.split(".")
			if len(data_path_parts) <= 1:
				continue

			property_name = data_path_parts[-1]
			match = re.search(r'key_blocks\["(.*)"\]', driver.data_path)
			if not match:
				continue

			shape_key_name = match.group(1)
			driver_data = {
				"driver": driver,
				"property": property_name,
			}

			if shape_key_name not in drivers:
				drivers[shape_key_name] = []
			drivers[shape_key_name].append(driver_data)

		return drivers

	def restore_shape_key_drivers(obj, copy_obj, drivers):
		shape_keys = obj.data.shape_keys
		if not shape_keys:
			return
		if not shape_keys.animation_data:
			shape_keys.animation_data_create()

		for shape_key_name, shape_key_drivers in drivers.items():
			shape_key_block = shape_keys.key_blocks.get(shape_key_name)
			if not shape_key_block:
				continue

			for driver_data in shape_key_drivers:
				source_fcurve = driver_data["driver"]
				property_name = driver_data["property"]

				try:
					new_driver = shape_key_block.driver_add(property_name)
					new_driver.driver.type = source_fcurve.driver.type

					if source_fcurve.driver.expression:
						new_driver.driver.expression = source_fcurve.driver.expression

					for var in source_fcurve.driver.variables:
						new_var = new_driver.driver.variables.new()
						new_var.name = var.name
						new_var.type = var.type

						for idx, target in enumerate(var.targets):
							new_var.targets[idx].id_type = target.id_type
							target_id = obj if target.id == copy_obj else target.id
							new_var.targets[idx].id = target_id
							new_var.targets[idx].data_path = target.data_path
							new_var.targets[idx].bone_target = target.bone_target
							new_var.targets[idx].transform_type = target.transform_type
							new_var.targets[idx].transform_space = target.transform_space
				except Exception:
					continue

	def copy_shape_key_animation(source_obj, target_obj):
		source_shape_keys = source_obj.data.shape_keys
		target_shape_keys = target_obj.data.shape_keys
		if not source_shape_keys or not target_shape_keys:
			return
		if not source_shape_keys.animation_data:
			return
		if not source_shape_keys.animation_data.action:
			return

		if not target_shape_keys.animation_data:
			target_shape_keys.animation_data_create()
		target_shape_keys.animation_data.action = source_shape_keys.animation_data.action

		if bpy.app.version >= (4, 4, 0):
			target_shape_keys.animation_data.action_slot = source_shape_keys.animation_data.action_slot

	def join_as_shape(temp_obj, original_obj, shape_key_name):
		temp_shape = temp_obj.shape_key_add(from_mix=False)
		new_shape = original_obj.shape_key_add(name=shape_key_name, from_mix=False)
		for source_vert, target_vert in zip(temp_shape.data, new_shape.data):
			target_vert.co = source_vert.co

	def cleanup_object(obj):
		obj_data = obj.data
		bpy.data.objects.remove(obj, do_unlink=True)
		if obj_data and obj_data.users == 0:
			bpy.data.meshes.remove(obj_data)

	shape_keys = mesh.data.shape_keys
	shapes_count = len(shape_keys.key_blocks) if shape_keys else 0
	if shapes_count == 0:
		apply_modifier_to_object(mesh, selected_modifiers)
		return True

	if shapes_count == 1:
		mesh.shape_key_remove(mesh.data.shape_keys.key_blocks[0])
		apply_modifier_to_object(mesh, selected_modifiers)
		return True

	pin_setting = mesh.show_only_shape_key
	saved_active_shape_key_index = mesh.active_shape_key_index
	use_relative = mesh.data.shape_keys.use_relative
	eval_time = mesh.data.shape_keys.eval_time

	if saved_active_shape_key_index == 0:
		mesh.active_shape_key_index = 1

	copy_obj = duplicate_object(mesh)
	copy_obj.hide_set(True)

	shape_key_properties = save_shape_key_properties(mesh)
	shape_key_drivers = save_shape_key_drivers(copy_obj)
	basis_name = copy_obj.data.shape_keys.key_blocks[0].name

	bpy.context.view_layer.objects.active = mesh
	bpy.ops.object.mode_set(mode="OBJECT")
	bpy.ops.object.select_all(action="DESELECT")
	mesh.select_set(True)
	bpy.ops.object.shape_key_remove(all=True)

	apply_modifier_to_object(mesh, selected_modifiers)
	mesh.shape_key_add(name=basis_name, from_mix=False)

	for i, key_block_name in enumerate(shape_key_properties.keys()):
		bpy.context.view_layer.objects.active = copy_obj
		temp_obj = duplicate_object(copy_obj)
		temp_obj.hide_set(True)
		bpy.context.view_layer.update()

		temp_obj.show_only_shape_key = True
		temp_obj.active_shape_key_index = i + 1
		shape_key_name = temp_obj.active_shape_key.name

		for modifier in temp_obj.modifiers:
			modifier.show_viewport = False

		apply_modifier_to_object(temp_obj, selected_modifiers)

		if len(mesh.data.vertices) != len(temp_obj.data.vertices):
			cleanup_object(temp_obj)
			cleanup_object(copy_obj)
			raise Exception(
				f"{shape_key_name} failed because the vertex count changed after applying modifiers on {mesh.name}"
			)

		join_as_shape(temp_obj, mesh, key_block_name)
		cleanup_object(temp_obj)

	shape_keys = mesh.data.shape_keys
	shape_keys.use_relative = use_relative
	shape_keys.eval_time = eval_time

	restore_shape_key_properties(mesh, shape_key_properties)
	copy_shape_key_animation(copy_obj, mesh)
	restore_shape_key_drivers(mesh, copy_obj, shape_key_drivers)

	mesh.show_only_shape_key = pin_setting
	mesh.active_shape_key_index = min(saved_active_shape_key_index, len(mesh.data.shape_keys.key_blocks) - 1)
	bpy.context.view_layer.objects.active = mesh

	cleanup_object(copy_obj)
	return True


def bakeArmatureModifier(mesh):
	arm_mod = getArmatureModifier(mesh)
	if not arm_mod:
		return

	if mesh.data.shape_keys:
		applyModifiersWithShapeKeys(mesh, [arm_mod.name])
		return

	bpy.context.view_layer.objects.active = mesh
	bpy.ops.object.mode_set(mode="OBJECT")
	bpy.ops.object.select_all(action="DESELECT")
	mesh.select_set(True)
	bpy.ops.object.modifier_apply(modifier=arm_mod.name)


def createMidpointBone(skeleton, mesh, parent, child, name, middle=True):
	# ensure parent and child exist to avoid AttributeError on .head/.tail
	if parent is None or child is None:
		print(f"Skipping createMidpointBone for '{name}': parent={getattr(parent, 'name', None)}, child={getattr(child, 'name', None)}")
		return None

	new_bone = skeleton.new(name)
	new_bone.head = child.head.copy()
	new_bone.tail = child.tail.copy()
	new_bone.parent = parent
	
	if middle:
		child.parent = new_bone
	
	head_midpoint = (parent.head + child.head) / 2
	tail_midpoint = (parent.tail + child.tail) / 2
	new_bone.head = head_midpoint
	new_bone.tail = tail_midpoint

	vg = mesh.vertex_groups.get(name) if mesh else None
	if not vg and mesh:
		print(f"Creating vertex group: {name}")
		mesh.vertex_groups.new(name=name)
	return new_bone

def disconnectBones(arm):
	bpy.context.view_layer.objects.active = arm
	bpy.ops.object.mode_set(mode="EDIT")
	for bone in arm.data.edit_bones:
		if bone.name not in protected_bones and bone.use_connect:
			bone.use_connect = False

# ======================================================
# Main functions
# 1. Get the armatures and meshes from the Input collection

meshes = []
arm_a = None
for obj in bpy.data.collections["Input"].all_objects:
	print(f"{obj.name}: {obj.type}")
	if obj.type == "ARMATURE":
		obj.hide_set(False)
		arm_a = obj
		continue
	
	if obj.type == "MESH" and not obj.name.endswith("_physics"):
		mods = [m for m in obj.modifiers if m.type == "ARMATURE"]
		if not mods:
			Exception(f"{obj.name} doesn't have an armature modifier")
		if not mods[0].object:
			Exception(f"{obj.name}'s armature modifier isn't pointing to an armature")

		obj.hide_set(False)
		meshes.append(obj)
		continue



if not arm_a:
	raise Exception(f"No armature in Input found")

ARMATURE_B = "Human Citizen Armature"
arm_b = bpy.data.objects.get(ARMATURE_B)
if not arm_b:
	raise Exception(f"Armature \"{ARMATURE_B}\" not found")

if not meshes:
	raise Exception(f"No meshes in Input found")


# Apply all non-ARMATURE modifiers on each mesh up-front (preserve shape keys)
for mesh in meshes:
	bpy.context.view_layer.objects.active = mesh
	bpy.ops.object.mode_set(mode="OBJECT")
	mods_to_apply = [m.name for m in mesh.modifiers if m.type != "ARMATURE"]
	if not mods_to_apply:
		continue
	print(f"Applying non-ARMATURE modifiers on {mesh.name}: {mods_to_apply}")
	try:
		# If mesh has shape keys, use the preservation helper
		if mesh.data.shape_keys:
			applyModifiersWithShapeKeys(mesh, mods_to_apply)
		else:
			# Apply modifiers in the stack order (first -> last)
			for mod_name in list(mods_to_apply):
				if mesh.modifiers.get(mod_name):
					bpy.ops.object.modifier_apply(modifier=mod_name)
	except Exception as e:
		print(f"Warning: failed to apply modifiers on {mesh.name}: {e}")




# disconnect any connected bones in the Human Citizen armature
disconnectBones(arm_b)



bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")


# 2. Handle the different number of possible spine bones

arm_a.select_set(True)
bpy.context.view_layer.objects.active = arm_a
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.armature.select_all(action="DESELECT")
bones_a = arm_a.data.edit_bones

# Determine spine candidate names from the first block keys in the rename table.
# This lets users put custom names (e.g. bone_spine) in that table and have
# this section follow those names automatically.
spine0_candidates = [k for k, v in bone_rename_table.items() if v == "spine_0"]
spine1_candidates = [k for k, v in bone_rename_table.items() if v == "spine_1"]
spine2_candidates = [k for k, v in bone_rename_table.items() if v == "spine_2"]

def _matches_any(bone_name, candidates):
	for c in candidates:
		if not c:
			continue
		if bone_name == c or bone_name.startswith(c):
			return True
	return False

def _natural_sort_key(name):
	import re
	return [int(part) if part.isdigit() else part.lower() for part in re.split(r"(\d+)", name)]

def _spine_group(name):
	if _matches_any(name, spine0_candidates):
		return 0
	if _matches_any(name, spine1_candidates):
		return 1
	if _matches_any(name, spine2_candidates):
		return 2
	return 3

# Collect spine bones present on the input armature and force a stable order:
# spine_0 source key(s) first, then spine_1, then spine_2.
spine_bones = [
	b.name
	for b in arm_a.data.edit_bones
	if _matches_any(b.name, spine0_candidates)
	or _matches_any(b.name, spine1_candidates)
	or _matches_any(b.name, spine2_candidates)
]
spine_bones = sorted(
	dict.fromkeys(spine_bones),
	key=lambda name: (_spine_group(name), _natural_sort_key(name)),
)

# Fallback: if no matches found using the table, fall back to a generic
# "spine" prefix search (case-insensitive) so older rigs still work.
if not spine_bones:
	spine_bones = sorted(
		[b.name for b in arm_a.data.edit_bones if b.name.lower().startswith("spine")],
		key=_natural_sort_key,
	)

# If there are more than 3 spine bones, try to merge the middle ones by
# sharing weight paint between the two middle candidates (spine_0 <-> spine_1)
if len(spine_bones) > 3:
	spine0_name = next((n for n in spine0_candidates if bones_a.get(n)), None)
	spine1_name = next((n for n in spine1_candidates if bones_a.get(n)), None)

	if spine0_name and spine1_name:
		for mesh in meshes:
			vg_bone = mesh.vertex_groups.get(spine0_name)
			vg_parent = mesh.vertex_groups.get(spine0_name)
			vg_child = mesh.vertex_groups.get(spine1_name)

			if not vg_bone:
				continue

			if not vg_parent:
				vg_parent = mesh.vertex_groups.new(name=spine0_name)
			if not vg_child:
				vg_child = mesh.vertex_groups.new(name=spine1_name)

			for v in mesh.data.vertices:
				try:
					w = vg_bone.weight(v.index)
				except RuntimeError:
					w = 0.0

				if w > 0:
					vg_parent.add([v.index], w * 0.5, "ADD")
					vg_child.add([v.index], w * 0.5, "ADD")

		# Reparent the two middle bones so they effectively act as one.
		if bones_a.get(spine0_name) and bones_a.get(spine1_name):
			bones_a.get(spine0_name).parent = bones_a.get(spine1_name)



spine_counter = 0
bones_a = arm_a.data.edit_bones

# rename the Input armature's spine bones and vertex groups in numerical order
for spine_bone in spine_bones:
	if spine_bone in bones_a:
		bones_a[spine_bone].name = f"spine_{spine_counter}"
		for mesh in meshes:
			for vg in mesh.vertex_groups:
				if (vg.name == spine_bone):
					vg.name = f"spine_{spine_counter}"
		spine_counter += 1

# Keep the existing Human Citizen spine_2 when the input only provides spine_0/spine_1.
preserve_output_spine_2 = not bool(bones_a.get("spine_2"))








# 3. Rename Input armature's bones and vertex groups to Human Citizen bones and vertex groups

for bone in arm_a.data.edit_bones:
	if (bone.name in bone_rename_table):
		bone.name = bone_rename_table[bone.name]

for mesh in meshes:
	for vg in mesh.vertex_groups:
		if (vg.name in bone_rename_table):
			vg.name = bone_rename_table[vg.name]







bpy.ops.object.mode_set(mode="EDIT")
for side in ["L","R"]:
	# 4. Create metacarpal bones in Input armature

	if not bones_a.get(f"finger_pinky_meta_{side}"):
		print(f"Creating finger_pinky_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_pinky_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_pinky_meta_{side}")


	if not bones_a.get(f"finger_ring_meta_{side}"):
		print(f"Creating finger_ring_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_ring_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_ring_meta_{side}")


	if not bones_a.get(f"finger_middle_meta_{side}"):
		print(f"Creating finger_middle_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_middle_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_middle_meta_{side}")


	if not bones_a.get(f"finger_index_meta_{side}"):
		print(f"Creating finger_index_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_index_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_index_meta_{side}")



	# 5. Create missing joint helper bones in Input armature

	if not bones_a.get(f"arm_elbow_helper_{side}"):
		parent = bones_a.get(f"arm_lower_{side}")
		if not parent:
			continue

		print(f"Creating joint helper bone: arm_elbow_helper_{side}")
		
		new_bone = bones_a.new(f"arm_elbow_helper_{side}")
		new_bone.head = parent.head.copy()
		new_bone.tail = parent.tail.copy()
		new_bone.parent = parent

	if not bones_a.get(f"leg_knee_helper_{side}"):
		parent = bones_a.get(f"leg_lower_{side}")
		if not parent:
			continue
		
		print(f"Creating joint helper bone: leg_knee_helper_{side}")

		new_bone = bones_a.new(f"leg_knee_helper_{side}")
		new_bone.head = parent.head.copy()
		new_bone.tail = parent.tail.copy()
		new_bone.parent = parent



	# 6. Create twist bones in arms and legs in Input armature

	# create upper twist bones in the appenditures
	if not bones_a.get(f"arm_upper_{side}_twist0"):
		bone = bones_a.get(f"arm_upper_{side}")
		if bone:
			print(f"Creating upper twist bone: arm_upper_{side}_twist0")

			new_bone = bones_a.new(f"arm_upper_{side}_twist0")
			new_bone.head = bone.head.copy()
			new_bone.tail = bone.tail.copy()
			new_bone.parent = bone
		else:
			print(f"Skipping arm_upper_{side}_twist0: source bone 'arm_upper_{side}' missing")

	if not bones_a.get(f"arm_lower_{side}_twist0"):
		bone = bones_a.get(f"arm_lower_{side}")
		if bone:
			print(f"Creating upper twist bone: arm_lower_{side}_twist0")

			new_bone = bones_a.new(f"arm_lower_{side}_twist0")
			new_bone.head = bone.head.copy()
			new_bone.tail = bone.tail.copy()
			new_bone.parent = bone
		else:
			print(f"Skipping arm_lower_{side}_twist0: source bone 'arm_lower_{side}' missing")

	if not bones_a.get(f"leg_upper_{side}_twist0"):
		bone = bones_a.get(f"leg_upper_{side}")
		if bone:
			print(f"Creating upper twist bone: leg_upper_{side}_twist0")

			new_bone = bones_a.new(f"leg_upper_{side}_twist0")
			new_bone.head = bone.head.copy()
			new_bone.tail = bone.tail.copy()
			new_bone.parent = bone
		else:
			print(f"Skipping leg_upper_{side}_twist0: source bone 'leg_upper_{side}' missing")

	if not bones_a.get(f"leg_lower_{side}_twist0"):
		bone = bones_a.get(f"leg_lower_{side}")
		if bone:
			print(f"Creating upper twist bone: leg_lower_{side}_twist0")

			new_bone = bones_a.new(f"leg_lower_{side}_twist0")
			new_bone.head = bone.head.copy()
			new_bone.tail = bone.tail.copy()
			new_bone.parent = bone
		else:
			print(f"Skipping leg_lower_{side}_twist0: source bone 'leg_lower_{side}' missing")


	# create lower twist bones in the midpoint of the appenditures
	if not bones_a.get(f"arm_upper_{side}_twist1"):
		print(f"Creating lower twist bone: arm_upper_{side}_twist1")

		parent = bones_a.get(f"arm_upper_{side}")
		child = bones_a.get(f"arm_lower_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"arm_upper_{side}_twist1", False)

	if not bones_a.get(f"arm_lower_{side}_twist1"):
		print(f"Creating lower twist bone: arm_lower_{side}_twist1")

		parent = bones_a.get(f"arm_lower_{side}")
		child = bones_a.get(f"hand_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"arm_lower_{side}_twist1", False)

	if not bones_a.get(f"leg_upper_{side}_twist1"):
		print(f"Creating lower twist bone: leg_upper_{side}_twist1")

		parent = bones_a.get(f"leg_upper_{side}")
		child = bones_a.get(f"leg_lower_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"leg_upper_{side}_twist1", False)

	if not bones_a.get(f"leg_lower_{side}_twist1"):
		print(f"Creating lower twist bone: leg_lower_{side}_twist1")

		parent = bones_a.get(f"leg_lower_{side}")
		child = bones_a.get(f"ankle_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"leg_lower_{side}_twist1", False)




# 7. Create any missing bones in the Human Citizen armature

bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_a.select_set(True)
arm_b.select_set(True)
bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="EDIT")
bones_a = arm_a.data.edit_bones
bones_b = arm_b.data.edit_bones

# create any missing bones that are in the Input armature but not the Human Citizen armature
for src_bone in bones_a:
	name = src_bone.name
	tgt_bone = bones_b.get(name)
	if not tgt_bone:
		print(f"Creating bone in {arm_b.name}: {name}")

		src_head_world = arm_a.matrix_world @ src_bone.head
		src_tail_world = arm_a.matrix_world @ src_bone.tail

		new_head = arm_b.matrix_world.inverted() @ src_head_world
		new_tail = arm_b.matrix_world.inverted() @ src_tail_world

		tgt_bone = bones_b.new(name)

		tgt_bone.head = new_head
		tgt_bone.tail = new_tail
		tgt_bone.roll = src_bone.roll

# set up the parents of the new bones
for src_bone in bones_a:
	tgt_bone = bones_b.get(src_bone.name)
	if src_bone.parent and src_bone.parent.name in bones_b:
		tgt_bone.parent = bones_b[src_bone.parent.name]

# If the input has only two spine bones, keep the output spine_2 and move upper-body children to it.
if preserve_output_spine_2:
	spine_1_bone = bones_b.get("spine_1")
	spine_2_bone = bones_b.get("spine_2")
	if spine_1_bone and spine_2_bone:
		if spine_2_bone.parent != spine_1_bone:
			spine_2_bone.parent = spine_1_bone
		for child_bone in bones_b:
			if child_bone.name == "spine_2" or child_bone.name.startswith("spine_"):
				continue
			if child_bone.parent == spine_1_bone:
				child_bone.parent = spine_2_bone


bones_a = arm_a.data.edit_bones
bones_b = arm_b.data.edit_bones

# remove any vestigial bones in the Human Citizen armature that don't exist in the Input armature
for src_bone in bones_b:
	tgt_bone = bones_a.get(src_bone.name)
	if not tgt_bone and src_bone.name not in protected_bones:
		if preserve_output_spine_2 and src_bone.name == "spine_2":
			continue
		print(f"Removing bone from {arm_b.name}: {src_bone.name}")
		arm_b.data.edit_bones.remove(src_bone)





# copy Human Citizen armature to create a reference armature
arm_c = arm_b.copy()
arm_c.data = arm_b.data.copy()
arm_c.name = "Reference Armature"
bpy.context.scene.collection.children.get("Output").objects.link(arm_c)




# 8. Move the Human Citizen armature's bones to match the Input armature in edit mode

arm_a.select_set(True)
arm_b.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bones_a = arm_a.data.edit_bones
bones_b = arm_b.data.edit_bones

for name, src_bone in bones_a.items():
	tgt_bone = bones_b.get(name)
	if not tgt_bone:
		continue

	old_dir = (tgt_bone.tail - tgt_bone.head).normalized()
	old_len = tgt_bone.length

	src_head_world = arm_a.matrix_world @ src_bone.head
	new_head = arm_b.matrix_world.inverted() @ src_head_world

	tgt_bone.head = new_head
	tgt_bone.tail = tgt_bone.head + old_dir * old_len






# 9. Move the Input meshes to the Human Citizen armature

# set the meshes's armature to the Human Citizen skeleton
for mesh in meshes:
	for mod in mesh.modifiers:
		if mod.type == "ARMATURE":
			mod.object = bpy.data.objects.get(ARMATURE_B)
			break


	# pair the mesh to the Human Citizen skeleton
	bpy.ops.object.mode_set(mode="OBJECT")
	
	mesh_matrix = mesh.matrix_world.copy()
	mesh.parent = arm_b
	mesh.matrix_world = mesh_matrix

	coll_target = bpy.context.scene.collection.children.get("Output")
	coll_target.objects.link(mesh)
	coll_input = bpy.context.scene.collection.children.get("Input")
	coll_input.objects.unlink(mesh)







# 10. Weight paint the metacarpals in the Human Citizen armature

metacarpals = [
	"finger_pinky_meta_R",
	"finger_ring_meta_R",
	"finger_middle_meta_R",
	"finger_index_meta_R",
	"finger_pinky_meta_L",
	"finger_ring_meta_L",
	"finger_middle_meta_L",
	"finger_index_meta_L",
]

# automatically weight paint the metacarpals
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_b.select_set(True)
for mesh in meshes:
	mesh.select_set(True)

bpy.ops.object.mode_set(mode="POSE")
bones_b = arm_b.pose.bones
for bone in metacarpals:
	if bone in bones_b:
		bones_b[bone].select = True

for mesh in meshes:
	bpy.context.view_layer.objects.active = mesh
	bpy.ops.object.mode_set(mode="WEIGHT_PAINT")
	bpy.ops.paint.weight_from_bones(type="AUTOMATIC")
	bpy.ops.object.vertex_group_limit_total(limit=4) # source 2 only allows 4 joint weights per vertex

# 11. Pose the Human Citizen armature to match the reference armature shape

bone_point_table = {
	"neck_0": "head",

	#"spine_0": "spine_1", # sometimes people but multiple spine bones in the same spot so just don't try to align them, they're basically always pointing up anyway
	#"spine_1": "spine_2",
	#"spine_2": "",


	"clavicle_R": "arm_upper_R",
	"arm_upper_R": "arm_lower_R",
	"arm_upper_R_twist0": "arm_lower_R",
	"arm_upper_R_twist1": "arm_lower_R",
	
	"arm_lower_R_twist0": "hand_R",
	"arm_lower_R_twist1": "hand_R",
	"arm_lower_R": "hand_R",
	
	"hand_R": "",

	"finger_pinky_meta_R": "finger_pinky_0_R",
	"finger_pinky_0_R": "finger_pinky_1_R",
	"finger_pinky_1_R": "finger_pinky_2_R",
	"finger_pinky_2_R": "",
	
	"finger_ring_meta_R": "finger_ring_0_R",
	"finger_ring_0_R": "finger_ring_1_R",
	"finger_ring_1_R": "finger_ring_2_R",
	"finger_ring_2_R": "",

	"finger_middle_meta_R": "finger_middle_0_R",
	"finger_middle_0_R": "finger_middle_1_R",
	"finger_middle_1_R": "finger_middle_2_R",
	"finger_middle_2_R": "",

	"finger_index_meta_R": "finger_index_0_R",
	"finger_index_0_R": "finger_index_1_R",
	"finger_index_1_R": "finger_index_2_R",
	"finger_index_2_R": "",

	"finger_thumb_0_R": "finger_thumb_1_R",
	"finger_thumb_1_R": "finger_thumb_2_R",
	"finger_thumb_2_R": "",


	"clavicle_L": "arm_upper_L",
	"arm_upper_L": "arm_lower_L",
	"arm_upper_L_twist0": "arm_lower_L",
	"arm_upper_L_twist1": "arm_lower_L",
	
	"arm_lower_L_twist0": "hand_L",
	"arm_lower_L_twist1": "hand_L",
	"arm_lower_L": "hand_L",
	
	"hand_L": "",

	"finger_pinky_meta_L": "finger_pinky_0_L",
	"finger_pinky_0_L": "finger_pinky_1_L",
	"finger_pinky_1_L": "finger_pinky_2_L",
	"finger_pinky_2_L": "",
	
	"finger_ring_meta_L": "finger_ring_0_L",
	"finger_ring_0_L": "finger_ring_1_L",
	"finger_ring_1_L": "finger_ring_2_L",
	"finger_ring_2_L": "",

	"finger_middle_meta_L": "finger_middle_0_L",
	"finger_middle_0_L": "finger_middle_1_L",
	"finger_middle_1_L": "finger_middle_2_L",
	"finger_middle_2_L": "",

	"finger_index_meta_L": "finger_index_0_L",
	"finger_index_0_L": "finger_index_1_L",
	"finger_index_1_L": "finger_index_2_L",
	"finger_index_2_L": "",

	"finger_thumb_0_L": "finger_thumb_1_L",
	"finger_thumb_1_L": "finger_thumb_2_L",
	"finger_thumb_2_L": "",


	"leg_upper_R": "leg_lower_R",
	"leg_upper_R_twist0": "leg_lower_R",
	"leg_upper_R_twist1": "leg_lower_R",

	"leg_lower_R": "ankle_R",
	"leg_lower_R_twist0": "ankle_R",
	"leg_lower_R_twist1": "ankle_R",

	#"ankle_R": "ball_R",


	"leg_upper_L": "leg_lower_L",
	"leg_upper_L_twist0": "leg_lower_L",
	"leg_upper_L_twist1": "leg_lower_L",

	"leg_lower_L": "ankle_L",
	"leg_lower_L_twist0": "ankle_L",
	"leg_lower_L_twist1": "ankle_L",

	#"ankle_L": "ball_L",
}

bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.armature.select_all(action="DESELECT")
bones_b = arm_b.data.edit_bones

last_direction = 0
for bone in bone_point_table.keys():
	bpy.ops.object.mode_set(mode="EDIT")
	
	src_bone = bones_b.get(bone)
	if not src_bone:
		continue

	# point parent to a child so that the bones line up with the mesh
	if (bone_point_table[bone] != ""):
		tgt_bone = bones_b.get(bone_point_table[bone])
		if not tgt_bone:
			src_head = src_bone.head.copy()
			length = src_bone.length
			src_bone.tail = src_head + last_direction * length
			continue
	
		src_head = src_bone.head.copy()
		length = src_bone.length
		direction = (tgt_bone.head - src_head).normalized()
		src_bone.tail = src_head + direction * length

		last_direction = direction

	# if no target then just point in the same direction as parent
	if (bone_point_table[bone] == ""):
		src_head = src_bone.head.copy()
		length = src_bone.length
		src_bone.tail = src_head + last_direction * length





bpy.context.view_layer.objects.active = arm_c
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_b.select_set(True)
arm_c.select_set(True)

# fix the rotations of the Human Citizen armature so that they match the reference armature
bpy.ops.object.mode_set(mode="EDIT")
for bone in bone_point_table.keys():
	if bone in arm_b.data.edit_bones:
		bpy.ops.armature.select_all(action="DESELECT")
	
		arm_b.data.edit_bones[bone].select = True
		arm_c.data.edit_bones[bone].select = True
		arm_c.data.edit_bones.active = arm_c.data.edit_bones[bone]

		bpy.ops.armature.calculate_roll(type="ACTIVE") # fix rotation bullshit where bones spin when their tail approaches 0 on an axis
bpy.ops.armature.select_all(action="DESELECT")




# pose the Human Citizen armature in the A-pose with no significant rotational differences to the reference armature, you want the difference between the Human Citizen armature and the reference armature to only be translational and scalable
bpy.ops.object.mode_set(mode="POSE")
for bone in bone_point_table.keys():
	if bone in arm_b.pose.bones:
		crr = arm_b.pose.bones[bone].constraints.new("COPY_ROTATION")
		crr.target = arm_c
		crr.name = "exp_rot"
		crr.subtarget = bone



# apply changes in pose mode to meshes
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
for mesh in meshes:
	bpy.context.view_layer.objects.active = mesh
	mesh.select_set(True)

	# merge armature with mesh permanently, preserving existing shape keys
	bakeArmatureModifier(mesh)

	# create new armature on mesh
	mod = mesh.modifiers.new(name="Armature", type="ARMATURE")
	mod.object = arm_b


# set posed bones to be the rest pose
bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.armature_apply(selected=False)










# 12. Realign miscellaneous bones in the Human Citizen armature

ik_realign_table = {
	"hand_R_IK_target": "hand_R",
	"hand_R_to_L_ikrule": "hand_R",

	"hand_L_IK_target": "hand_L",
	"hand_L_to_R_ikrule": "hand_L",

	"foot_R_IK_target": "ankle_R",

	"foot_L_IK_target": "ankle_L",
}

bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="EDIT")
bones_b = arm_b.data.edit_bones

# realign the IK bones
for src_bone_name in ik_realign_table.keys():
	bone_src = bones_b.get(src_bone_name)
	bone_target = bones_b.get(ik_realign_table[src_bone_name])
	if not bone_src or not bone_target:
		print(f"Skipping IK realign for '{src_bone_name}': source or target missing (src={getattr(bone_src,'name',None)}, tgt={getattr(bone_target,'name',None)})")
		continue

	bone_src_length = bone_src.length

	bone_src.head = bone_target.head.copy()
	bone_src.tail = bone_target.tail.copy()
	bone_src.roll = bone_target.roll
	bone_src.length = bone_src_length

# realign the hold bones
for side in ["L","R"]:
	bone_src = bones_b.get(f"hold_{side}")
	if not bone_src:
		print(f"Skipping hold_{side} realign: bone missing")
		continue
	bone_src.roll = math.radians(-90)

	# try to reliably recreate the hold position regardless of model proportions
	bone_target = bones_b.get(f"finger_thumb_0_{side}")
	if bone_target:
		bone_src.head = bone_target.head.copy()
		bone_src.tail = bone_src.head
		bone_src.head.y -= 7
		bone_src.tail.y = bone_src.head.y - 10
		continue

	bone_target = bones_b.get(f"hand_{side}")
	if bone_target:
		bone_src.head = bone_target.head.copy() + (bone_target.tail - bone_target.head).normalized() * 2
		bone_src.tail = bone_src.head
		bone_src.head.y -= 10
		bone_src.tail.y = bone_src.head.y - 10
		continue

	bone_target = bones_b.get(f"ValveBiped.Anim_Attachment_{side}H")
	if bone_target:
		bone_src.head = bone_target.head.copy()
		bone_src.tail = bone_src.head
		bone_src.tail.y = bone_src.head.y - 10







if (not EYES_ENABLED):
	if "eye_L" in bones_b:
		bones_b["eye_L"].name = "eye_L_disabled"
	if "eye_R" in bones_b:
		bones_b["eye_R"].name = "eye_R_disabled"



# 13. Export the Human Citizen armature and meshes

bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_b.select_set(True)
for mesh in meshes:
	mesh.select_set(True)
	mesh.hide_viewport = False

export_path = os.path.join(bpy.path.abspath("//"),  os.path.splitext(os.path.basename(bpy.data.filepath))[0] + ".fbx")
bpy.ops.export_scene.fbx(
	filepath=export_path,
	use_selection=True,
	object_types={"ARMATURE", "MESH"},
	add_leaf_bones=False,
	primary_bone_axis="X",
	secondary_bone_axis="Z"
)



# 14. Cleanup

bpy.context.view_layer.layer_collection.children["Input"].exclude = True
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
bpy.context.view_layer.objects.active = arm_c
arm_c.select_set(True)
bpy.ops.object.delete()

# remove all copy rotation constraints
bpy.context.view_layer.objects.active = arm_b
for pbone in arm_b.pose.bones:
	if pbone.constraints:
		for c in pbone.constraints:
			pbone.constraints.remove(c)


# remove all unused vertex groups
bones_b_names = [b.name for b in arm_b.data.bones]
for mesh in meshes:
	for vg in [vg for vg in mesh.vertex_groups if vg.name not in bones_b_names]:
		print(f"Removing unused vertex group in {mesh.name}: {vg.name}")
		mesh.vertex_groups.remove(vg)

# ======================================================
# The End :)
