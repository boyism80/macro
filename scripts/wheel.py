import stone

def receive_epona(app):
	app.KeyPress(('ALT', 'J'))

	found = app.Detect('epona-main')

	app.Click((425, 240))
	app.Sleep(500)
	app.Click((425, 350))

	app.Click((1285, 355))
	app.Sleep(500)
	app.Click((1285, 430))
	app.Sleep(500)
	app.Click((1285, 505))
	app.Sleep(500)
	app.Escape()

def bifrost_1(app):
	app.KeyPress(('ALT', 'W'))
	app.Detect('bifrost-main')
	app.Click((1470, 385))
	app.Enter()
	app.Sleep(1000)

	app.Detect('loa-talk')

def fishing(app, mini_game=True):
	pivot = app.GetCursorPosition()
	while True:
		found = app.Detect('gear-crash', timeout=0)
		if found:
			app.KeyPress(('ALT', '`'))
			
			found = app.Detect('pet-repair-icon')
			app.Click(found['pet-repair-icon']['position'])

			found = app.Detect('repair-all')
			app.Click(found['repair-all']['position'])

			app.Enter()
			app.Escape()
			app.Escape()

		found = app.Detect('fishing-buff', area={"x": 696, "y": 1017, "width": 202, "height": 51}, timeout=0)
		if found:
			app.KeyPress('D')
			app.Sleep(2500)

		prev = app.SetCursorPosition(pivot)
		app.KeyPress('W')
		app.Sleep(1000)
		app.SetCursorPosition(prev)

		found = app.Detect('not-enough-energy', timeout=1000*3)
		if 'not-enough-energy' in found:
			return


			app.Click((1033, 927))

			potions = ('life-energy-potion(large)', 'life-energy-potion(normal)', 'life-energy-potion(small)')
			found = app.Detect(potions, timeout=500)
			if not found:
				break

			if 'life-energy-potion(small)' in found:
				app.Click((1074, 564))
			elif 'life-energy-potion(normal)' in found:
				app.Click((1074, 640))
			elif 'life-energy-potion(large)' in found:
				app.Click((1074, 713))

			app.Click((950, 770))
			app.Escape()
			continue

		candidates = ('fish-catch', 'fish-fail', 'fishing-mini-game') if mini_game else ('fish-catch', 'fish-fail')
		found = app.Detect(candidates, timeout=1000*30)
		if 'fish-catch' in found:
			app.KeyPress('W')
			app.Sleep(5200)
			if mini_game:
				app.KeyPress('F')
			app.Sleep(800)
		elif 'fish-fail' in found:
			yield 'failed'
			app.Sleep(2000)
		elif 'fishing-mini-game' in found:
			yield 'mini game start'
			app.Sleep(14000)
			yield 'mini game end'
		else:
			pass

def stone_simulate(app, slot, engraving_name=None, counts=(5,6), with_pheon=True):
	if with_pheon:
		stone.buy_pheon(app)

	for count in counts:
		stone.buy_stone(app, slot, count)
		app.KeyPress('G')
		if stone.facet_stone(app, engraving_name, count):
			return
			
		stone.disassemble_stones(app)

def callback(app):
	return fishing(app, mini_game=False)

	# for i in range(1):
	# 	stone_simulate(app, 2, engraving_name='adrenaline', with_pheon=False, counts=(4, ))