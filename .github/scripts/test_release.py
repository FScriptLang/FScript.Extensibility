"""Exercise first-release and subsequent-release planning without touching the real repo."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

SCRIPTS = Path(__file__).resolve().parent

class ReleaseTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        subprocess.run(['git', 'init', '-q', '-b', 'main', self.root], check=True)
        self.git('config', 'user.email', 'tests@example.invalid')
        self.git('config', 'user.name', 'Release tests')
        (self.root / 'CHANGELOG.md').write_text('# Changelog\n\n## [Unreleased]\n\n- Initial SDK.\n')
        (self.root / 'Directory.Build.props').write_text('<Project><AssemblyVersion>1.0.0.0</AssemblyVersion></Project>')
        shutil.copy(SCRIPTS / 'release.py', self.root)
        self.commit()
        self.bootstrap = self.git('rev-parse', 'HEAD').strip()

    def git(self, *args):
        return subprocess.check_output(['git', *args], cwd=self.root, text=True, stderr=subprocess.STDOUT)

    def commit(self):
        self.git('add', '.')
        self.git('commit', '-qm', 'chore: test release inputs')

    def preview(self, version):
        return subprocess.run(['python3', 'release.py', version, 'true'], cwd=self.root, text=True, capture_output=True)

    def test_bootstrap_compare_and_no_mutation(self):
        result = self.preview('1.0.0')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f'{self.bootstrap}...1.0.0', result.stdout)
        self.assertEqual(self.git('status', '--porcelain'), '')
        self.assertEqual(self.git('tag'), '')

    def test_subsequent_compare(self):
        self.git('tag', '1.0.0')
        result = self.preview('1.0.1')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('1.0.0...1.0.1', result.stdout)

    def test_major_release_requires_updated_assembly_version(self):
        self.git('tag', '1.0.0')
        (self.root / 'Directory.Build.props').write_text('<Project><AssemblyVersion>2.0.0.0</AssemblyVersion></Project>')
        self.commit()
        result = self.preview('2.0.0')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('1.0.0...2.0.0', result.stdout)

    def test_invalid_versions_and_dirty_tree(self):
        for version in ['1.0.0-beta', '2.0.0', '1.0.1']:
            self.assertNotEqual(self.preview(version).returncode, 0)
        (self.root / 'dirty').write_text('pending')
        self.assertNotEqual(self.preview('1.0.0').returncode, 0)

    def test_release_notes_require_bullets_and_compare(self):
        (self.root / 'CHANGELOG.md').write_text('# Changelog\n\n## [Unreleased]\n\n## [1.0.0]\n\n- SDK.\n')
        command = ['python3', str(SCRIPTS / 'notes.py'), '1.0.0']
        self.assertNotEqual(subprocess.run(command, cwd=self.root, capture_output=True).returncode, 0)
        with (self.root / 'CHANGELOG.md').open('a') as stream:
            stream.write('\n**Full Changelog**: https://github.com/MagnusOpera/FScript.Extensibility/compare/bootstrap...1.0.0\n')
        result = subprocess.run(command, cwd=self.root, text=True, capture_output=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('- SDK.', result.stdout)

if __name__ == '__main__':
    unittest.main()
