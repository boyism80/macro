import random
import datetime

def throw_fish_rod(vmodel):
	if vmodel.Status['fishing'] == 'throw':
		return False

	random_wichman = random.WichmannHill(datetime.datetime.now())
	rand_width, rand_height = random_wichman.randint(-10, 10), random_wichman.randint(-10, 10)

	x, y, width, height = vmodel.App.PyArea
	vmodel.App.StoreCursorPosition(width * 0.7 + rand_width, height * 0.2 + rand_height)
	vmodel.App.KeyPress('W', 200)
	vmodel.App.RestoreCursorPosition()
	vmodel.Status['fishing'] = 'throw'
	vmodel.Status['throw time'] = datetime.datetime.now()
	return True

def pull_fish_rod(vmodel):
	if vmodel.Status['fishing'] == 'pull':
		return False

	vmodel.App.KeyPress('W')
	vmodel.Status['fishing'] = 'pull'
	return True

def detect_catch(vmodel, frame):
	x, y, width, height = vmodel.App.PyArea
	begin, size = (width * 0.3, height * 0.35), (width * 0.3, height * 0.3)

	return vmodel.Sprite['fishing icon'].MatchTo(frame, begin, size)

def detect_reduce(vmodel, frame):
	throwing_time = datetime.datetime.now() - vmodel.Status['throw time']
	detection_failed = vmodel.Status['fishing'] == 'throw' and throwing_time.seconds > 5
	
	if vmodel.Status['fishing'] != 'pull' and not detection_failed:
		return

	x, y, width, height = vmodel.App.PyArea
	begin, size = (width * 0.36, height * 0.68), (width * 0.24, height * 0.08)
	return vmodel.Sprite['reduce icon'].MatchTo(frame, begin, size)

def wait_next_action(vmodel):
	if vmodel.Status['fishing'] == 'idle':
		return None

	vmodel.SetTimer('fishing', 1000, 'fishing.py')
	vmodel.Status['fishing'] = 'idle'