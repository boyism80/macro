import detect

def contains_point_in_rect(rect, point):
	x, y, width, height = rect
	point_x, point_y = point

	if point_x < x or point_x > x + width:
		return False

	if point_y < y or point_y > y + height:
		return False

	return True

def is_clicked_area(clicked_location_list, area):
	for x in clicked_location_list:
		if contains_point_in_rect(area, x):
			return True

	return False

def callback(vmodel, frame, parameter):
	detection_area_list_left, detection_area_list_right = vmodel.State['detection area list couple']
	try:
		if len(detection_area_list_left) == 0:
			vmodel.State['detection area list couple'] = None
			vmodel.State['done'] = True

		else:
			detection_area_left = detection_area_list_left[0]
			detection_area_right = detection_area_list_right[0]

			x, y, width, height = detection_area_left

			vmodel.App.Click((x, y))
				
			detection_area_list_left.remove(detection_area_left)
			detection_area_list_right.remove(detection_area_right)
			vmodel.AddHistory('Click location : ' + str((x, y)))
			vmodel.SetTimer('click detection', 100, 'click_detection.py')
	except Exception as e:
		vmodel.AddHistory('exception : ' + str(e))
		return str(e)
