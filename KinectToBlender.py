bl_info = {
    "name": "Motion from Azure Kinect",
    "blender": (2, 80, 0),
    "category": "Object",
}
from os import close
import bpy
import bpy_extras.io_utils
import json
import mathutils
from bpy.types import Armature, Pose, PoseBone

class ReadLocationsFromFile(bpy.types.Operator,bpy_extras.io_utils.ImportHelper):
    """Capture Motion From Mocap Text File."""      # Use this as a tooltip for menu items and buttons.
    bl_idname = "armature.position_bones"        # Unique identifier for buttons and menu items to reference.
    bl_label = "Reposition Armature Bones"         # Display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}  # Enable undo for the operator.

    #json format for bone info: {"Timestamp":double,"Name":string,"X":double,"Y":double,"Z":double,"W":double,"XLoc":double,"YLoc":double,"ZLoc":double}


    def execute(self, context:bpy.context):        # execute() is called when running the operator.
        bonedict = {
            "Pelvis": "spine",
            "SpineNavel": "spine.002",
            "SpineChest": "spine.003",
            "Neck": "spine.004",
            "ClavicleLeft":"shoulder.L",
            "ClavicleRight":"shoulder.R",
            "ShoulderLeft":"upper_arm.L",
            "ShoulderRight":"upper_arm.R",
            "ElbowLeft":"forearm.L",
            "ElbowRight":"forearm.R",
            "WristLeft":"hand.L",
            "WristRight":"hand.R",
            "HandLeft":"hand.L",
            "HandRight":"hand.R",
            "HandTipLeft":"hand.L",
            "HandTipRight":"hand.R",
            "ThumbLeft":"hand.L",
            "ThumbRight":"hand.R",
            "HipLeft":"thigh.L",
            "HipRight":"thigh.R",
            "KneeLeft":"shin.L",
            "KneeRight":"shin.R",
            "AnkleLeft":"foot.L",
            "AnkleRight":"foot.R",
            "FootLeft":"toe.L",
            "FootRight":"toe.R",
            "Head":"spine.006",
            "Nose":"toe.L",
            "EyeLeft":"toe.L",
            "EyeRight":"toe.L",
            "EarLeft":"toe.L",
            "EarRight":"toe.L"
        }
        inputfilepath :str = self.filepath
        print(inputfilepath+" opening")
        scene = context.scene
        armature:Armature = scene.objects['armature']
        armature.select_set(True)
        bpy.ops.object.mode_set(mode = 'POSE')
        file = open(inputfilepath)
        previous_quaternion = [1,0,0,0]
        i = 0
        while i < 32:
                bonejsonline:str = file.readline()
                bone = json.loads(bonejsonline)
           # try:
                hbone : PoseBone = armature.pose.bones[bonedict[bone['Name']]]
                #hbone.rotation_mode = 'AXIS_ANGLE'
                #hbone.rotation_axis_angle = [float(bone['W']),float(bone['X']),float(bone['Y']),float(bone['Z'])]
                hbone.rotation_mode = 'QUATERNION'
                hbone.rotation_quaternion = [float(bone['W']),float(bone['X']),float(bone['Y']),float(bone['Z'])] #rotation is global while blender rotation is local. Requires transformation.
                #if bone['Name'] == 'HipRight': # or 'KneeRight' or 'AnkleRight' or 'FootRight'): #right leg coordinate space has inverted x and y 
                 #   euler_rot :mathutils.Euler = mathutils.Quaternion(hbone.rotation_quaternion).to_euler('XYZ')
                 #   euler_rot.z = euler_rot.z - 180
                 #   euler_rot.y = euler_rot.y - 180
                 #   hbone.rotation_quaternion = euler_rot.to_quaternion()
                 #   hbone.rotation_quaternion = [-float(bone['W']),float(bone['X']),float(bone['Y']),-float(bone['Z'])]
                    #hbone.rotation_quaternion = mathutils.Quaternion(hbone.rotation_quaternion).inverted() #rotation is global while blender rotation is local. Requires transformation.
                parent_quaternion = [float(bone['PW']),float(bone['PX']),float(bone['PY']),float(bone['PZ'])]

                if bone['Name'] != 'Pelvis':
                    inverted = mathutils.Quaternion(parent_quaternion).inverted()
                    hbone.rotation_quaternion = hbone.rotation_quaternion @ inverted #transform from global to local space by multiplying by inverse of parent transform
                   
                   # hbone.rotation_axis_angle = 
                    
                    print(bone['Name'] + hbone.rotation_quaternion.__str__() +"hip inverted" + mathutils.Quaternion(hbone.rotation_quaternion).inverted().__str__() + " "+ inverted.__str__())
                        #rot_invert:mathutils.Quaternion = hbone.rotation_quaternion@ mathutils.Quaternion(hbone.rotation_quaternion).inverted()
                     #   hbone.rotation_quaternion = [float(bone['W']),float(bone['X']),float(bone['Y']),float(bone['Z'])]
                        #hbone.rotation_quaternion = hbone.rotation_quaternion @ inverted
                #debone = Matrix([[1,0,0,0],[0,0,-1,0], [0,1,0,0],[0,0,0,1]])
                #previous_quaternion = hbone.rotation_quaternion
                #hbone.rotation_quaternion = hbone.rotation_quaternion @ previous_quaternion
       #     except any as err:
       #         print(err)
           # finally:
                i+=1
           #     continue
        file.close()
        return {'FINISHED'}            # Lets Blender know the operator finished successfully.

def menu_func(self, context:bpy.context):
    self.layout.operator(ReadLocationsFromFile.bl_idname)

def read_line(line :str):
    values = line.split(' ')
    

def register():
    bpy.utils.register_class(ReadLocationsFromFile)
    bpy.types.VIEW3D_MT_object.append(menu_func)  # Adds the new operator to an existing menu.

def unregister():
    bpy.utils.unregister_class(ReadLocationsFromFile)



if __name__ == "__main__":
    register()
    #test: invert y and w on right leg
    #todo: adjust mocap data to scale of armature
    #todo: adjust armature to scale of mocap data
    #todo: adjust mocap data to account for floor/gravity orientation, likely in C# code using gyroscope/IMU