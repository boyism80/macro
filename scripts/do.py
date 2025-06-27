def guild_donation(app):
	app.KeyPress(('ALT', 'U'))

	found = app.Detect(('guild-donation', 'attendance-reward-info', 'guild-create'), 0.9)
	if 'guild-create' in found:
		app.Escape()
		return

	if 'attendance-reward-info' in found:
		app.Escape()
		app.Sleep(500)

	app.Click(found['guild-donation']['position'])

	found = app.Detect(('donation', 'donation-disabled'), 0.7, {'x': 600, 'y': 550, 'width': 180, 'height': 50})
	if 'donation' in found:
		app.Click(found['donation']['position'])
		app.Sleep(500)
		app.Escape()
		app.Sleep(500)

	app.Escape()

	found = app.Detect('research-support', untilDetect=False)
	if 'research-support' in found:
		app.Click(found['research-support']['position'])

		app.Detect('research-support-main')
		found = app.Detect('cannot-support', area={'x': 700, 'y': 500, 'width': 200, 'height': 100}, untilDetect=False)
		if 'cannot-support' in found:
			app.Escape()
		else:
			app.Click((825, 535))
			app.Sleep(250)
			app.Click((900, 755))
			app.Sleep(500)

	app.Escape()

def daily_homework_rotation(app):
	character_slots = [(860, 400), (1120, 400), (600, 520), (860, 400), (1120, 400)]
	for character_slot in character_slots:
		app.Escape()
		found = app.Detect('change-character')
		app.Click(found['change-character']['position'])
		app.Click(character_slot)
		app.Sleep(500)

		found = app.Detect(('connect-enabled', 'connect-disabled'))
		if 'connect-enabled' not in found:
			continue
		
		app.Click(found['connect-enabled']['position'])

		found = app.Detect('connect-confirm')
		app.Click(found['connect-confirm']['position'])

		found = app.Detect('event-info'5, {'x': 920, 'y': 180, 'width': 100, 'height': 25})
		app.Escape()

		guild_donation(app)

		epona(app)

def enter_pw(app, pw):
	sprite_names = []
	for c in str(pw):
		sprite_name = 'num-' + c
		sprite_names.append(sprite_name)
	sprite_names = tuple(set(sprite_names))

	found = app.Detect(sprite_names)
	for c in str(pw):
		sprite_name = 'num-' + c
		app.Click(found[sprite_name]['position'])

def buy_pheon(app):
	app.KeyPress(115) # F4
	found = app.Detect('convenience')
	app.Click(found['convenience']['position'])

	found = app.Detect('pheon')
	app.Click(found['pheon']['position'])

	found = app.Detect('buy')
	app.Click(found['buy']['position'])

	found = app.Detect('buy-step-1')
	app.Click(found['buy-step-1']['position'])

	found = app.Detect('buy-step-2')
	app.Click(found['buy-step-2']['position'])

	enter_pw(app, 557575)

	app.Click((985, 680))
	found = app.Detect('connect-confirm')
	app.Click(found['connect-confirm']['position'])

	found = app.Detect('accept-reward')
	app.Click(found['accept-reward']['position'])

	found = app.Detect('accept-reward-confirm')
	app.Click(found['accept-reward-confirm']['position'])

	found = app.Detect('connect-confirm')
	app.Click(found['connect-confirm']['position'])

	app.Escape()
	app.Escape()
	app.KeyPress('I')

	found = app.Detect('pheon-icon')
	app.RClick(found['pheon-icon']['position'], ('ALT',))
	app.Enter()
	app.Escape()

def dialog_until_finish(app):
	app.KeyPress('G')
	app.Detect('quest-dialog')
	while True:
		found = app.Detect(('quest-dialog', 'loa-talk'))
		if 'quest-dialog' not in found:
			break

		app.KeyPress('G')

def show_bifrost(app):
	found = app.Detect('bifrost')
	app.Click(found['bifrost']['position'])
	app.Detect('bifrost-main')

def epona_mokomoko_night_market(app):
	dialog_wait_ms = 5000

	app.Detect('moko-moko-night-market')
	dialog_until_finish(app)

	app.RClick((400, 925))
	app.Sleep(2000)

	app.KeyPress('G')
	app.Sleep(dialog_wait_ms)

	app.RClick((1050, 925))
	app.Sleep(1500)

	app.KeyPress('G')
	app.Sleep(dialog_wait_ms)

	app.RClick((1380, 775))
	app.Sleep(1500)

	app.KeyPress('G')
	app.Sleep(dialog_wait_ms)

	app.RClick((1000, 100))
	app.Sleep(2000)

	app.RClick((980, 300))
	app.Sleep(1500)
	dialog_until_finish(app)

def epona_dark_forest(app):
	app.Detect('dark-forest')
	app.KeyPress('G')
	app.Sleep(4000)

	app.RClick((1200, 940))
	app.Sleep(2000)
	app.RClick((900, 915))
	app.Sleep(1500)

	app.KeyPress('G')
	app.Sleep(4000)

def epona(app):
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

	app.KeyPress(('ALT', 'W'))
	app.Click((1470, 385))
	app.Enter()
	epona_mokomoko_night_market(app)

	# show_bifrost(app)
	# found = app.Detect('no-named-rift')
	# found['no-named-rift']['position'] = (found['no-named-rift']['position'][0] + 300, found['no-named-rift']['position'][1])
	# app.Click(found['no-named-rift']['position'])
	# app.Enter()

	# epona_dark_forest(app)

def show_remote_post(app):
	app.KeyPress(('ALT', '`'))
	found = app.Detect('remote-post')
	app.Click(found['remote-post']['position'])


# -*- coding: utf-8 -*-
def callback(app):
	# dialog_until_finish(app)

	# return daily_homework_rotation(app)
	# return buy_pheon(app)

	# app.KeyPress(('ALT', 'W'))
	# app.KeyPress(('ALT', 'U'))
	pass