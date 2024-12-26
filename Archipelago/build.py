import shutil
shutil.make_archive('momo4', 'zip', root_dir='.', base_dir='momo4')
shutil.move('momo4.zip', '../build/momo4.apworld')