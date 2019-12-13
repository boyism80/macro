from base_fishing import *

def callback(vmodel, frame, parameter):
	vmodel.Status['fishing'] = 'idle'
	return throw_fish_rod(vmodel)