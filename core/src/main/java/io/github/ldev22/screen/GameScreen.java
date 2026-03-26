package io.github.ldev22.screen;

import com.badlogic.gdx.Screen;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.OrthographicCamera;
import com.badlogic.gdx.graphics.glutils.ShapeRenderer;
import com.badlogic.gdx.utils.viewport.FitViewport;
import com.badlogic.gdx.utils.viewport.Viewport;
import io.github.ldev22.config.GameConfig;
import io.github.ldev22.entity.Player;
import io.github.ldev22.utils.DebugCameraController;
import io.github.ldev22.utils.GdxGraphics;

public class GameScreen implements Screen {

    private OrthographicCamera camera;
    private Viewport viewport;
    private ShapeRenderer renderer;
    private Player player;
    private DebugCameraController dCamera;
    @Override
    public void show() {
        camera = new OrthographicCamera();
        viewport = new FitViewport(GameConfig.WORLD_WIDTH, GameConfig.WORLD_HEIGHT, camera);
        renderer = new ShapeRenderer();
        player = new Player();
        dCamera = new DebugCameraController();
        dCamera.setStartPosition(GameConfig.WORLD_CENTER_X, GameConfig.WORLD_CENTER_Y);
        float startPlayerX = GameConfig.WORLD_WIDTH / 2f;

        player.setPosition(startPlayerX, 1);
    }

    @Override
    public void render(float delta) {
        dCamera.handleDebugInput();
        dCamera.applyToCamera(camera);
        player.update();
        GdxGraphics.clearScreen(Color.BLACK);
        renderer.setColor(Color.ORANGE);
        renderer.setProjectionMatrix(camera.combined);
        player.drawDebug(renderer);
        GdxGraphics.drawGrid(viewport, renderer, 1);
    }

    @Override
    public void resize(int width, int height) {

    }

    @Override
    public void pause() {

    }

    @Override
    public void resume() {

    }

    @Override
    public void hide() {

    }

    @Override
    public void dispose() {
        renderer.dispose();
    }
}
