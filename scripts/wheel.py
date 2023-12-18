import stone

def receive_epona(app):
	app.target.KeyPress(('ALT', 'J'))

	found = app.Detect('epona-main')

	app.target.Click((425, 240))
	app.Sleep(500)
	app.target.Click((425, 350))

	app.target.Click((1285, 355))
	app.Sleep(500)
	app.target.Click((1285, 430))
	app.Sleep(500)
	app.target.Click((1285, 505))
	app.Sleep(500)
	app.target.Escape()

def bifrost_1(app):
	app.target.KeyPress(('ALT', 'W'))
	app.Detect('bifrost-main')
	app.target.Click((1470, 385))
	app.target.Enter()
	app.Sleep(1000)

	app.Detect('loa-talk')

def fishing(app):
	pivot = app.target.GetCursorPosition()
	while True:
		found = app.Detect('gear-crash', timeout=0)
		if found:
			app.target.KeyPress(('ALT', '`'))
			
			found = app.Detect('pet-repair-icon')
			app.target.Click(found['pet-repair-icon']['position'])

			found = app.Detect('repair-all')
			app.target.Click(found['repair-all']['position'])

			app.target.Enter()
			app.target.Escape()
			app.target.Escape()

		prev = app.target.SetCursorPosition(pivot)
		app.target.KeyPress('W')
		app.target.SetCursorPosition(prev)

		found = app.Detect('not-enough-energy', timeout=1000*3)
		if 'not-enough-energy' in found:
			app.target.Click((1033, 927))

			potions = ('life-energy-potion(large)', 'life-energy-potion(normal)', 'life-energy-potion(small)')
			found = app.Detect(potions, timeout=500)
			if not found:
				break

			if 'life-energy-potion(small)' in found:
				app.target.Click((1074, 564))
			elif 'life-energy-potion(normal)' in found:
				app.target.Click((1074, 640))
			elif 'life-energy-potion(large)' in found:
				app.target.Click((1074, 713))

			found = app.Detect('use_enegy_potion')
			app.target.Click(found['use_enegy_potion']['position'])
			app.target.Escape()
			continue

		found = app.Detect(('fish-catch', 'fish-fail'), area={"x": 864, "y": 425, "width": 197, "height": 170}, timeout=1000*30)
		if 'fish-catch' in found:
			yield (f"{found['fish-catch']['percent']:.2f}", found['fish-catch']['position'])
			
			app.target.KeyPress('W')
			app.Sleep(6000)
		elif 'fish-fail' in found:
			yield 'failed'
			app.Sleep(2000)
		else:
			pass

def stone_simulate(app, slot, engraving_name=None, counts=(5,6), with_pheon=True):
	if with_pheon:
		stone.buy_pheon(app)

	for count in counts:
		stone.buy_stone(app, slot, count)
		app.target.KeyPress('G')
		if stone.facet_stone(app, engraving_name, count):
			return
			
		stone.disassemble_stones(app)

def callback(app):
	return fishing(app)
	# for i in range(1):
	# 	stone_simulate(app, 2, engraving_name='adrenaline', with_pheon=False, counts=(4, ))